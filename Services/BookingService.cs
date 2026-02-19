using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace BarberDario.Api.Services;

public class BookingService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly EmailService _emailService;

    public BookingService(
        SkinbloomDbContext context,
        ILogger<BookingService> logger,
        EmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<BookingResponseDto> CreateBookingAsync(CreateBookingDto dto)
    {
        // 1. Validiere Service
        var service = await _context.Services.FindAsync(dto.ServiceId);
        if (service == null || !service.IsActive)
            throw new ArgumentException("Service nicht gefunden oder inaktiv");

        // 2. Parse Datum und Zeit
        var bookingDate = DateOnly.Parse(dto.BookingDate);
        var startTime = TimeOnly.Parse(dto.StartTime);
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // 3. Prüfe ob Zeitslot noch verfügbar ist
        var isSlotAvailable = await IsSlotAvailableAsync(dto.ServiceId, bookingDate, startTime, endTime);
        if (!isSlotAvailable)
            throw new InvalidOperationException("Dieser Zeitslot ist bereits gebucht");

        var bookingDateTime = bookingDate.ToDateTime(startTime);

        // 4. Prüfe maximalen Vorlauf
        var maxAdvanceDays = await GetSettingValueAsync("MAX_ADVANCE_BOOKING_DAYS", 60);
        if (bookingDate > DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(maxAdvanceDays)))
            throw new InvalidOperationException($"Termine können maximal {maxAdvanceDays} Tage im Voraus gebucht werden");

        // 5. Normalize empty strings → null for Email/Phone
        var email = string.IsNullOrWhiteSpace(dto.Customer.Email) ? null : dto.Customer.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(dto.Customer.Phone) ? null : dto.Customer.Phone.Trim();

        // 6. Find or create customer (lookup by email OR phone)
        Customer? customer = null;

        if (email != null)
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email == email);

        if (customer == null && phone != null)
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone != null && c.Phone == phone);

        if (customer == null)
        {
            customer = new Customer
            {
                FirstName = dto.Customer.FirstName,
                LastName = dto.Customer.LastName,
                Email = email,
                Phone = phone
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer.FirstName = dto.Customer.FirstName;
            customer.LastName = dto.Customer.LastName;
            if (email != null) customer.Email = email;
            if (phone != null) customer.Phone = phone;
            customer.UpdatedAt = DateTime.UtcNow;
        }

        // 7. Resolve employee (optional)
        Employee? employee = null;
        if (dto.EmployeeId.HasValue)
        {
            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.IsActive);
        }

        // 8. Erstelle Buchung
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ServiceId = service.Id,
            EmployeeId = employee?.Id,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed,
            CustomerNotes = dto.CustomerNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);

        customer.TotalBookings++;
        customer.LastVisit = DateTime.SpecifyKind(bookingDateTime, DateTimeKind.Utc);
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking created: {BookingId} for customer {Email} on {Date} at {Time}",
            booking.Id, customer.Email, bookingDate, startTime);

        // 9. Sende Bestätigungs-Email
        if (!string.IsNullOrEmpty(customer.Email))
        {
            try
            {
                await _emailService.SendBookingConfirmationAsync(booking.Id);
                booking.ConfirmationSentAt = DateTime.UtcNow;

                _context.EmailLogs.Add(new EmailLog
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    RecipientEmail = customer.Email,
                    EmailType = EmailType.Confirmation,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Sent
                });
                await _context.SaveChangesAsync();
                _logger.LogInformation("Confirmation email sent to {Email}", customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", customer.Email);

                _context.EmailLogs.Add(new EmailLog
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    RecipientEmail = customer.Email,
                    EmailType = EmailType.Confirmation,
                    SentAt = null,
                    Status = EmailStatus.Failed,
                    ErrorMessage = ex.Message
                });
                await _context.SaveChangesAsync();
            }
        }

        return new BookingResponseDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            booking.Status.ToString(),
            booking.ConfirmationSentAt.HasValue,
            new BookingDetailsDto(
                service.Id,
                service.Name,
                bookingDate.ToString("yyyy-MM-dd"),
                startTime.ToString("HH:mm"),
                endTime.ToString("HH:mm"),
                service.Price
            ),
            new CustomerDto(customer.FirstName, customer.LastName, customer.Email),
            employee == null ? null : new EmployeeDto(employee.Id, employee.Name, employee.Role, employee.Specialty)
        );
    }

    public async Task<BookingResponseDto?> GetBookingByIdAsync(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return null;

        return new BookingResponseDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            booking.Status.ToString(),
            booking.ConfirmationSentAt.HasValue,
            new BookingDetailsDto(
                booking.Service.Id,
                booking.Service.Name,
                booking.BookingDate.ToString("yyyy-MM-dd"),
                booking.StartTime.ToString("HH:mm"),
                booking.EndTime.ToString("HH:mm"),
                booking.Service.Price
            ),
            new CustomerDto(
                booking.Customer.FirstName,
                booking.Customer.LastName,
                booking.Customer.Email
            ),
            new EmployeeDto(
                booking.Employee?.Id ?? Guid.Empty,
                booking.Employee?.Name ?? "N/A",
                booking.Employee?.Role ?? "N/A",
                booking.Employee?.Specialty ?? "N/A"
            )
        );
    }

    public async Task<List<BookingResponseDto>> GetBookingsByEmailAsync(string email)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Where(b => b.Customer.Email.ToLower() == email.ToLower())
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.StartTime)
            .ToListAsync();

        return bookings.Select(booking => new BookingResponseDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            booking.Status.ToString(),
            booking.ConfirmationSentAt.HasValue,
            new BookingDetailsDto(
                booking.Service.Id,
                booking.Service.Name,
                booking.BookingDate.ToString("yyyy-MM-dd"),
                booking.StartTime.ToString("HH:mm"),
                booking.EndTime.ToString("HH:mm"),
                booking.Service.Price
            ),
            new CustomerDto(
                booking.Customer.FirstName,
                booking.Customer.LastName,
                booking.Customer.Email
            ),
            new EmployeeDto(
                booking.Employee?.Id ?? Guid.Empty,
                booking.Employee?.Name ?? "N/A",
                booking.Employee?.Role ?? "N/A",
                booking.Employee?.Specialty ?? "N/A"
            )
        )).ToList();
    }

    public async Task<CancelBookingResponseDto> CancelBookingAsync(Guid id, CancelBookingDto dto)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
        {
            throw new ArgumentException("Buchung nicht gefunden");
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException("Buchung ist bereits storniert");
        }

        if (booking.Status == BookingStatus.Completed)
        {
            throw new InvalidOperationException("Abgeschlossene Buchungen können nicht storniert werden");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        booking.CancellationReason = dto.Reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking cancelled: {BookingId}, Reason: {Reason}",
            booking.Id, dto.Reason
        );

        // Sende Stornierungsbestätigung wenn gewünscht
        bool emailSent = false;
        if (dto.NotifyCustomer)
        {
            try
            {
                await _emailService.SendCancellationConfirmationAsync(booking, booking.Customer, booking.Service);

                // Log Email
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.Cancellation,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Sent
                });

                await _context.SaveChangesAsync();
                emailSent = true;

                _logger.LogInformation("Cancellation email sent to {Email}", booking.Customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send cancellation email to {Email}", booking.Customer.Email);

                // Log Email Failure
                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = booking.Customer.Email,
                    EmailType = EmailType.Cancellation,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Failed,
                    ErrorMessage = ex.Message
                });

                await _context.SaveChangesAsync();
            }
        }

        return new CancelBookingResponseDto(
            true,
            "Termin erfolgreich storniert",
            emailSent
        );
    }

    // Add this method to your BookingService class
    public async Task<BookingResponseDto> ConfirmBookingAsync(Guid bookingId)
    {
        // 1. Find booking with customer and service details
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new ArgumentException("Buchung nicht gefunden");
        }

        // 2. Check if booking can be confirmed
        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Buchung kann nicht bestätigt werden. Aktueller Status: {booking.Status}");
        }

        // 3. Check if confirmation is still valid (within 24 hours)
        var hoursSinceCreation = (DateTime.UtcNow - booking.CreatedAt).TotalHours;
        if (hoursSinceCreation > 24)
        {
            throw new InvalidOperationException(
                "Bestätigungsfrist (24 Stunden) abgelaufen. Bitte kontaktieren Sie uns oder erstellen Sie eine neue Buchung.");
        }

        // 4. Check if booking date is not in the past
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (booking.BookingDate < today)
        {
            throw new InvalidOperationException(
                "Vergangene Buchungen können nicht bestätigt werden.");
        }

        // 5. Update booking status
        booking.Status = BookingStatus.Confirmed;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking confirmed: {BookingId} for customer {Email}",
            booking.Id, booking.Customer.Email
        );

        // 6. Send confirmation receipt
        bool emailSent = false;
        try
        {
            await _emailService.SendConfirmationReceiptAsync(booking, booking.Customer, booking.Service);

            _context.EmailLogs.Add(new EmailLog
            {
                BookingId = booking.Id,
                RecipientEmail = booking.Customer.Email,
                EmailType = EmailType.Confirmation,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Sent
            });

            await _context.SaveChangesAsync();
            emailSent = true;

            _logger.LogInformation("Confirmation receipt sent to {Email}", booking.Customer.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation receipt to {Email}", booking.Customer.Email);

            _context.EmailLogs.Add(new EmailLog
            {
                BookingId = booking.Id,
                RecipientEmail = booking.Customer.Email,
                EmailType = EmailType.Confirmation,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Failed,
                ErrorMessage = ex.Message
            });

            await _context.SaveChangesAsync();
        }

        // 7. Return updated booking
        return new BookingResponseDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            booking.Status.ToString(),
            emailSent,
            new BookingDetailsDto(
                booking.Service.Id,
                booking.Service.Name,
                booking.BookingDate.ToString("yyyy-MM-dd"),
                booking.StartTime.ToString("HH:mm"),
                booking.EndTime.ToString("HH:mm"),
                booking.Service.Price
            ),
            new CustomerDto(
                booking.Customer.FirstName,
                booking.Customer.LastName,
                booking.Customer.Email
            ),
           new EmployeeDto(
                booking.Employee?.Id ?? Guid.Empty,
                booking.Employee?.Name ?? "N/A",
                booking.Employee?.Role ?? "N/A",
                booking.Employee?.Specialty ?? "N/A"
           )
        );
    }

    private async Task<bool> IsSlotAvailableAsync(
        Guid serviceId,  // Parameter behalten für Service-Validierung
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        // FIX: Prüfe ALLE Buchungen, nicht nur den spezifischen Service!
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < endTime &&
                b.EndTime > startTime
            ); // ❌ KEINE ServiceId Filterung!

        if (hasConflict) return false;

        // Prüfe gesperrte Zeitslots
        var isBlocked = await _context.BlockedTimeSlots
            .AnyAsync(bs =>
                bs.BlockDate == date &&
                bs.StartTime < endTime &&
                bs.EndTime > startTime
            );

        return !isBlocked;
    }

    private async Task<int> GetSettingValueAsync(string key, int defaultValue)
    {
        var setting = await _context.Settings.FindAsync(key);
        if (setting == null) return defaultValue;

        return int.TryParse(setting.Value, out var value) ? value : defaultValue;
    }
}