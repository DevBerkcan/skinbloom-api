// Services/ManualBookingService.cs
using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class ManualBookingService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<ManualBookingService> _logger;
    private readonly EmailService _emailService;

    public ManualBookingService(
        SkinbloomDbContext context,
        ILogger<ManualBookingService> logger,
        EmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<ManualBookingResponseDto> CreateManualBookingAsync(CreateManualBookingDto dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new ArgumentException("Vorname ist erforderlich");

        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new ArgumentException("Nachname ist erforderlich");

        // Validate service
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.IsActive);

        if (service == null)
            throw new ArgumentException("Service nicht gefunden oder inaktiv");

        // Parse date and time
        if (!DateOnly.TryParse(dto.BookingDate, out var bookingDate))
            throw new ArgumentException("Ungültiges Datumsformat");

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
            throw new ArgumentException("Ungültiges Zeitformat");

        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // Check if slot is available
        var isAvailable = await IsSlotAvailableAsync(bookingDate, startTime, endTime);
        if (!isAvailable)
            throw new InvalidOperationException("Dieser Zeitslot ist nicht verfügbar");

        // Find or create customer
        var customer = await FindOrCreateCustomer(dto);

        // Create booking
        var booking = new Booking
        {
            CustomerId = customer.Id,
            ServiceId = service.Id,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed, // Auto-confirm for manual bookings
            CustomerNotes = dto.CustomerNotes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // Update customer stats
        customer.TotalBookings++;
        customer.LastVisit = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Try to send confirmation email if email is provided
        bool emailSent = false;
        if (!string.IsNullOrEmpty(customer.Email))
        {
            try
            {
                await _emailService.SendBookingConfirmationAsync(booking.Id);
                emailSent = true;

                _context.EmailLogs.Add(new EmailLog
                {
                    BookingId = booking.Id,
                    RecipientEmail = customer.Email,
                    EmailType = EmailType.Confirmation,
                    SentAt = DateTime.UtcNow,
                    Status = EmailStatus.Sent
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send confirmation email to {Email}", customer.Email);
            }
        }

        return new ManualBookingResponseDto(
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
            new CustomerBasicDto(
                customer.FirstName,
                customer.LastName,
                customer.Email
            )
        );
    }

    public async Task<ManualBookingResponseDto?> GetManualBookingByIdAsync(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return null;

        return new ManualBookingResponseDto(
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
            new CustomerBasicDto(
                booking.Customer.FirstName,
                booking.Customer.LastName,
                booking.Customer.Email
            )
        );
    }

    private async Task<Customer> FindOrCreateCustomer(CreateManualBookingDto dto)
    {
        // Try to find by email if provided
        if (!string.IsNullOrEmpty(dto.Email))
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower());

            if (existingCustomer != null)
            {
                // Update existing customer with new info if needed
                existingCustomer.FirstName = dto.FirstName;
                existingCustomer.LastName = dto.LastName;
                existingCustomer.Phone = dto.Phone ?? existingCustomer.Phone;
                existingCustomer.UpdatedAt = DateTime.UtcNow;

                return existingCustomer;
            }
        }

        // Try to find by phone if provided (and no email match)
        if (!string.IsNullOrEmpty(dto.Phone))
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == dto.Phone);

            if (existingCustomer != null)
            {
                existingCustomer.FirstName = dto.FirstName;
                existingCustomer.LastName = dto.LastName;
                existingCustomer.Email = dto.Email ?? existingCustomer.Email;
                existingCustomer.UpdatedAt = DateTime.UtcNow;

                return existingCustomer;
            }
        }

        // Create new customer
        return new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<bool> IsSlotAvailableAsync(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        // Check for conflicting bookings
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < endTime &&
                b.EndTime > startTime
            );

        if (hasConflict) return false;

        // Check for blocked slots
        var isBlocked = await _context.BlockedTimeSlots
            .AnyAsync(bs =>
                bs.BlockDate == date &&
                bs.StartTime < endTime &&
                bs.EndTime > startTime
            );

        return !isBlocked;
    }
}