using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class BookingService
{
    private readonly BarberDarioDbContext _context;
    private readonly ILogger<BookingService> _logger;
    private readonly EmailService _emailService;

    public BookingService(
        BarberDarioDbContext context,
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
        {
            throw new ArgumentException("Service nicht gefunden oder inaktiv");
        }

        // 2. Parse Datum und Zeit
        var bookingDate = DateOnly.Parse(dto.BookingDate);
        var startTime = TimeOnly.Parse(dto.StartTime);
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // 3. Prüfe ob Zeitslot noch verfügbar ist
        var isSlotAvailable = await IsSlotAvailableAsync(
            dto.ServiceId,
            bookingDate,
            startTime,
            endTime
        );

        if (!isSlotAvailable)
        {
            throw new InvalidOperationException("Dieser Zeitslot ist bereits gebucht");
        }

        // 4. Prüfe Mindestvorlauf
        var minAdvanceHours = await GetSettingValueAsync("MIN_ADVANCE_BOOKING_HOURS", 24);
        var bookingDateTime = bookingDate.ToDateTime(startTime);
        var minBookingTime = DateTime.UtcNow.AddHours(minAdvanceHours);

        if (bookingDateTime < minBookingTime)
        {
            throw new InvalidOperationException($"Termine müssen mindestens {minAdvanceHours} Stunden im Voraus gebucht werden");
        }

        // 5. Prüfe maximalen Vorlauf
        var maxAdvanceDays = await GetSettingValueAsync("MAX_ADVANCE_BOOKING_DAYS", 60);
        if (bookingDate > DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(maxAdvanceDays)))
        {
            throw new InvalidOperationException($"Termine können maximal {maxAdvanceDays} Tage im Voraus gebucht werden");
        }

        // 6. Finde oder erstelle Kunde
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == dto.Customer.Email);

        if (customer == null)
        {
            customer = new Customer
            {
                FirstName = dto.Customer.FirstName,
                LastName = dto.Customer.LastName,
                Email = dto.Customer.Email,
                Phone = dto.Customer.Phone
            };
            _context.Customers.Add(customer);
        }
        else
        {
            // Update Kundendaten falls geändert
            customer.FirstName = dto.Customer.FirstName;
            customer.LastName = dto.Customer.LastName;
            customer.Phone = dto.Customer.Phone;
            customer.UpdatedAt = DateTime.UtcNow;
        }

        // 7. Erstelle Buchung
        var booking = new Booking
        {
            CustomerId = customer.Id,
            ServiceId = dto.ServiceId,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Pending,
            CustomerNotes = dto.CustomerNotes
        };

        _context.Bookings.Add(booking);

        // 8. Update Customer Stats
        customer.TotalBookings++;
        customer.LastVisit = DateTime.SpecifyKind(bookingDateTime, DateTimeKind.Utc);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking created: {BookingId} for customer {Email} on {Date} at {Time}",
            booking.Id, customer.Email, bookingDate, startTime
        );

        // 9. Sende Bestätigungs-Email
        bool emailSent = false;
        try
        {
            await _emailService.SendBookingConfirmationAsync(booking, customer, service);
            booking.ConfirmationSentAt = DateTime.UtcNow;

            // Log Email
            _context.EmailLogs.Add(new EmailLog
            {
                BookingId = booking.Id,
                RecipientEmail = customer.Email,
                EmailType = EmailType.Confirmation,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Sent
            });

            await _context.SaveChangesAsync();
            emailSent = true;

            _logger.LogInformation("Confirmation email sent to {Email}", customer.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation email to {Email}", customer.Email);

            // Log Email Failure
            _context.EmailLogs.Add(new EmailLog
            {
                BookingId = booking.Id,
                RecipientEmail = customer.Email,
                EmailType = EmailType.Confirmation,
                SentAt = DateTime.UtcNow,
                Status = EmailStatus.Failed,
                ErrorMessage = ex.Message
            });

            await _context.SaveChangesAsync();
        }

        return new BookingResponseDto(
            booking.Id,
            booking.BookingNumber,
            booking.Status.ToString(),
            emailSent,
            new BookingDetailsDto(
                service.Id,
                service.Name,
                bookingDate.ToString("yyyy-MM-dd"),
                startTime.ToString("HH:mm"),
                endTime.ToString("HH:mm"),
                service.Price
            ),
            new CustomerDto(
                customer.FirstName,
                customer.LastName,
                customer.Email
            )
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
            booking.BookingNumber,
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
            booking.BookingNumber,
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

    private async Task<bool> IsSlotAvailableAsync(
        Guid serviceId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        // Prüfe existierende Buchungen
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.ServiceId == serviceId &&
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < endTime &&
                b.EndTime > startTime
            );

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
