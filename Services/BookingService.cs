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
        {
            throw new ArgumentException("Service nicht gefunden oder inaktiv");
        }

        // 2. Parse Datum und Zeit
        var bookingDate = DateOnly.Parse(dto.BookingDate);
        var startTime = TimeOnly.Parse(dto.StartTime);
        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // 3. Prüfe ob Zeitslot noch verfügbar ist - MIT TRANSAKTION!
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

        // 7. NOCHMAL Prüfen - DIREKT VOR DEM SPEICHERN!
        var finalCheck = await _context.Bookings
            .AnyAsync(b =>
                b.BookingDate == bookingDate &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < endTime &&
                b.EndTime > startTime
            ); 

        if (finalCheck)
        {
            throw new InvalidOperationException("Dieser Zeitslot wurde leider soeben von jemand anderem gebucht");
        }

        // 8. Erstelle Buchung
        var booking = new Booking
        {
            CustomerId = customer.Id,
            ServiceId = dto.ServiceId,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed, // Bleibt Confirmed
            CustomerNotes = dto.CustomerNotes,
            ConfirmationSentAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);

        // 9. Update Customer Stats
        customer.TotalBookings++;
        customer.LastVisit = DateTime.SpecifyKind(bookingDateTime, DateTimeKind.Utc);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking created: {BookingId} for customer {Email} on {Date} at {Time}",
            booking.Id, customer.Email, bookingDate, startTime
        );

        // 10. Sende Bestätigungs-Email
        bool emailSent = false;
        try
        {
            await _emailService.SendBookingConfirmationAsync(booking.Id);
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
            booking.BookingNumber,
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
