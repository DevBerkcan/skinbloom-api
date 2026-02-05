using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly BarberDarioDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        BarberDarioDbContext context,
        EmailService emailService,
        ILogger<TestController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Create dummy bookings for today (for testing timeline)
    /// </summary>
    [HttpGet("seed-today")]
    public async Task<IActionResult> SeedTodayBookings()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Get or create test customer
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == "test@barberdario.com");
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Test",
                    LastName = "Kunde",
                    Email = "test@barberdario.com",
                    Phone = "0123456789",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
            }

            // Get first service
            var service = await _context.Services.FirstOrDefaultAsync();
            if (service == null)
            {
                return BadRequest("No services found. Create services first.");
            }

            // Create bookings for different times today
            var bookingTimes = new[]
            {
                ("09:00", "09:30"),
                ("10:00", "10:30"),
                ("11:30", "12:00"),
                ("14:00", "14:30"),
                ("15:30", "16:00"),
                ("17:00", "17:30"),
                ("18:30", "19:00")
            };

            var createdBookings = new List<Booking>();

            foreach (var (start, end) in bookingTimes)
            {
                // Check if booking already exists
                var exists = await _context.Bookings.AnyAsync(b =>
                    b.BookingDate == today &&
                    b.StartTime == TimeOnly.Parse(start));

                if (!exists)
                {
                    var booking = new Booking
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        ServiceId = service.Id,
                        BookingDate = today,
                        StartTime = TimeOnly.Parse(start),
                        EndTime = TimeOnly.Parse(end),
                        Status = BookingStatus.Confirmed,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Bookings.Add(booking);
                    createdBookings.Add(booking);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Created {createdBookings.Count} test bookings for today",
                date = today.ToString("yyyy-MM-dd"),
                bookings = createdBookings.Select(b => new
                {
                    id = b.Id,
                    time = $"{b.StartTime} - {b.EndTime}",
                    status = b.Status
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding today bookings");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Test email sending (sends a test email to specified address)
    /// </summary>
    [HttpPost("send-test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            // Get first service and a booking for context
            var service = await _context.Services.FirstOrDefaultAsync();
            if (service == null)
            {
                return BadRequest("No services found");
            }

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName ?? "Test",
                LastName = request.LastName ?? "User",
                Email = request.Email,
                Phone = request.Phone ?? "0123456789",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                ServiceId = service.Id,
                BookingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                StartTime = TimeOnly.Parse("10:00"),
                EndTime = TimeOnly.Parse("10:30"),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _emailService.SendBookingConfirmationAsync(booking, customer, service);

            return Ok(new
            {
                message = "Test email sent successfully",
                recipient = request.Email,
                type = "Booking Confirmation"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test email to {Email}", request.Email);
            return StatusCode(500, new
            {
                message = "Failed to send email",
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Update services with real prices
    /// </summary>
    [HttpGet("update-services")]
    public async Task<IActionResult> UpdateServices()
    {
        try
        {
            // Get existing services
            var existingServices = await _context.Services.OrderBy(s => s.DisplayOrder).ToListAsync();

            // Service data
            var serviceData = new[]
            {
                ("Haircut", "Klassischer Herrenhaarschnitt mit Styling", 30, 44.00m, 1),
                ("Beard (traditional hot towel shave)", "Traditionelle Rasur mit heißem Handtuch", 20, 42.00m, 2),
                ("Hair & Beardcut", "Haarschnitt + Bart Trimmen", 50, 74.00m, 3),
                ("The Dario Standard", "Premium Komplettpaket - Haarschnitt, Bart, Styling & Pflege", 120, 130.00m, 4),
                ("Long Haircut / Restyle", "Langhaar-Schnitt oder komplettes Restyling", 60, 90.00m, 5)
            };

            var updatedServices = new List<Service>();

            // Update existing services or create new ones
            for (int i = 0; i < serviceData.Length; i++)
            {
                var (name, description, duration, price, displayOrder) = serviceData[i];

                if (i < existingServices.Count)
                {
                    // Update existing service
                    var service = existingServices[i];
                    service.Name = name;
                    service.Description = description;
                    service.DurationMinutes = duration;
                    service.Price = price;
                    service.DisplayOrder = displayOrder;
                    service.IsActive = true;
                    service.UpdatedAt = DateTime.UtcNow;
                    updatedServices.Add(service);
                }
                else
                {
                    // Create new service
                    var service = new Service
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Description = description,
                        DurationMinutes = duration,
                        Price = price,
                        IsActive = true,
                        DisplayOrder = displayOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Services.Add(service);
                    updatedServices.Add(service);
                }
            }

            // Deactivate extra services if any
            for (int i = serviceData.Length; i < existingServices.Count; i++)
            {
                existingServices[i].IsActive = false;
                existingServices[i].UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Services updated successfully",
                count = updatedServices.Count,
                services = updatedServices.Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    price = s.Price,
                    duration = s.DurationMinutes
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating services");
            return StatusCode(500, new {
                message = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Delete all test bookings
    /// </summary>
    [HttpGet("clear-test-bookings")]
    public async Task<IActionResult> ClearTestBookings()
    {
        try
        {
            var testCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == "test@barberdario.com");

            if (testCustomer != null)
            {
                var testBookings = await _context.Bookings
                    .Where(b => b.CustomerId == testCustomer.Id)
                    .ToListAsync();

                _context.Bookings.RemoveRange(testBookings);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Deleted {testBookings.Count} test bookings" });
            }

            return Ok(new { message = "No test bookings found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing test bookings");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

public class TestEmailRequest
{
    public string Email { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
}
