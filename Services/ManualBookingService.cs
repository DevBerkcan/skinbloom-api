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

        // Resolve optional employee
        Employee? employee = null;
        if (dto.EmployeeId.HasValue)
        {
            employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId.Value && e.IsActive);
        }

        // Create booking
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            ServiceId = service.Id,
            EmployeeId = employee?.Id,
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
            Booking.GenerateBookingNumber(bookingDate, booking.Id),
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
            Booking.GenerateBookingNumber(bookingDate, booking.Id),
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
            ),
            employee != null
                ? new EmployeeDto(employee.Id, employee.Name, employee.Role, employee.Specialty)
                : null
        );
    }

    public async Task<ManualBookingResponseDto?> GetManualBookingByIdAsync(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Include(b => b.Employee)
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
                booking.Customer.Email
            ),
            booking.Employee != null
                ? new EmployeeDto(booking.Employee.Id, booking.Employee.Name, booking.Employee.Role, booking.Employee.Specialty)
                : null
        );
    }

    private async Task<Customer> FindOrCreateCustomer(CreateManualBookingDto dto)
    {
        // Normalize: treat empty/whitespace as null so unique indexes work correctly
        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        var firstName = dto.FirstName.Trim();
        var lastName = dto.LastName.Trim();

        Customer? customer = null;

        // 1. Find by email (most reliable)
        if (email != null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == email.ToLower());

            if (customer != null)
                _logger.LogInformation("Found existing customer by email: {Email}", email);
        }

        // 2. Find by phone
        if (customer == null && phone != null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone);

            if (customer != null)
                _logger.LogInformation("Found existing customer by phone: {Phone}", phone);
        }

        // 3. Find by name (fallback)
        if (customer == null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.FirstName.ToLower() == firstName.ToLower() &&
                    c.LastName.ToLower() == lastName.ToLower());

            if (customer != null)
                _logger.LogInformation("Found existing customer by name: {FirstName} {LastName}", firstName, lastName);
        }

        if (customer != null)
        {
            // Update name
            customer.FirstName = firstName;
            customer.LastName = lastName;
            customer.UpdatedAt = DateTime.UtcNow;

            // Update email only if provided and not taken by another customer
            if (email != null && customer.Email != email)
            {
                var emailTaken = await _context.Customers
                    .AnyAsync(c => c.Email == email && c.Id != customer.Id);

                if (!emailTaken)
                    customer.Email = email;
            }

            // Update phone only if provided and different
            if (phone != null && customer.Phone != phone)
                customer.Phone = phone;

            _logger.LogInformation("Updated existing customer: {CustomerId}", customer.Id);
        }
        else
        {
            // Create new — store null, NOT empty string, for missing contact info
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,   // null if not provided
                Phone = phone,   // null if not provided
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TotalBookings = 0,
                NoShowCount = 0
            };

            _context.Customers.Add(customer);
            _logger.LogInformation("Created new customer: {FirstName} {LastName}", firstName, lastName);
        }

        return customer;
    }

    private async Task<bool> IsSlotAvailableAsync(DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        var hasConflict = await _context.Bookings
            .AnyAsync(b =>
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < endTime &&
                b.EndTime > startTime
            );

        if (hasConflict) return false;

        var isBlocked = await _context.BlockedTimeSlots
            .AnyAsync(bs =>
                bs.BlockDate == date &&
                bs.StartTime < endTime &&
                bs.EndTime > startTime
            );

        return !isBlocked;
    }
}