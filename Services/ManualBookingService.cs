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
            Id = Guid.NewGuid(),
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

        // Update customer stats
        customer.TotalBookings++;
        customer.LastVisit = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Manual booking created: {BookingNumber} for customer {CustomerId} - {FirstName} {LastName}",
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            customer.Id,
            customer.FirstName,
            customer.LastName
        );

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
                _logger.LogWarning(ex, "Could not send confirmation email to {Email}", customer.Email);
            }
        }

        return new ManualBookingResponseDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
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
                customer.Email ?? string.Empty
            ),
            new EmployeeDto(
                booking.Employee?.Id ?? Guid.Empty,
                booking.Employee?.Name ?? "N/A",
                booking.Employee?.Role ?? "N/A",
                booking.Employee?.Specialty ?? "N/A"
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
            new CustomerBasicDto(
                booking.Customer.FirstName,
                booking.Customer.LastName,
                booking.Customer.Email ?? string.Empty
            ),
            new EmployeeDto(
                booking.Employee?.Id ?? Guid.Empty,
                booking.Employee?.Name ?? "N/A",
                booking.Employee?.Role ?? "N/A",
                booking.Employee?.Specialty ?? "N/A"
            )
        );
    }

    private async Task<Customer> FindOrCreateCustomer(CreateManualBookingDto dto)
    {
        Customer? customer = null;

        // First try to find by email if provided (most reliable)
        if (!string.IsNullOrEmpty(dto.Email))
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == dto.Email.ToLower());

            if (customer != null)
            {
                _logger.LogInformation("Found existing customer by email: {Email}", dto.Email);
            }
        }

        // If not found by email, try by phone if provided
        if (customer == null && !string.IsNullOrEmpty(dto.Phone))
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == dto.Phone);

            if (customer != null)
            {
                _logger.LogInformation("Found existing customer by phone: {Phone}", dto.Phone);
            }
        }

        // If still not found, try by name combination (less reliable, but worth trying)
        if (customer == null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.FirstName.ToLower() == dto.FirstName.ToLower() &&
                    c.LastName.ToLower() == dto.LastName.ToLower());

            if (customer != null)
            {
                _logger.LogInformation("Found existing customer by name: {FirstName} {LastName}",
                    dto.FirstName, dto.LastName);
            }
        }

        if (customer != null)
        {
            // Update existing customer with new information
            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.UpdatedAt = DateTime.UtcNow;

            // Update email if provided and different
            if (!string.IsNullOrEmpty(dto.Email) && customer.Email != dto.Email)
            {
                // Check if email is already taken by another customer
                var emailExists = await _context.Customers
                    .AnyAsync(c => c.Email == dto.Email && c.Id != customer.Id);

                if (!emailExists)
                {
                    customer.Email = dto.Email;
                }
            }

            // Update phone if provided and different
            if (!string.IsNullOrEmpty(dto.Phone) && customer.Phone != dto.Phone)
            {
                customer.Phone = dto.Phone;
            }

            _logger.LogInformation("Updated existing customer: {CustomerId}", customer.Id);
        }
        else
        {
            // Create new customer
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email ?? string.Empty,
                Phone = dto.Phone ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalBookings = 0,
                NoShowCount = 0
            };

            _context.Customers.Add(customer);
            _logger.LogInformation("Created new customer: {FirstName} {LastName}",
                dto.FirstName, dto.LastName);
        }

        return customer;
    }

    private async Task<bool> IsSlotAvailableAsync(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        // Check for conflicting bookings (excluding cancelled ones)
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