using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly ManualBookingService _manualBookingService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AdminService adminService, ILogger<AdminController> logger, ManualBookingService manualBookingService)
    {
        _adminService = adminService;
        _logger = logger;
        _manualBookingService = manualBookingService;
    }

    /// <summary>
    /// Get dashboard overview with today's bookings, next booking, and statistics
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboard()
    {
        var dashboard = await _adminService.GetDashboardOverviewAsync();
        return Ok(dashboard);
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics()
    {
        var statistics = await _adminService.GetDashboardStatisticsAsync();
        return Ok(statistics);
    }

    /// <summary>
    /// Get paginated list of bookings with filters
    /// </summary>
    [HttpGet("bookings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponseDto<BookingListItemDto>>> GetBookings(
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] Guid? serviceId,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new BookingFilterDto(status, fromDate, toDate, serviceId, searchTerm, page, pageSize);
        var result = await _adminService.GetBookingsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Update booking status
    /// </summary>
    [HttpPatch("bookings/{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingListItemDto>> UpdateBookingStatus(
        Guid id,
        [FromBody] UpdateBookingStatusDto dto)
    {
        try
        {
            var booking = await _adminService.UpdateBookingStatusAsync(id, dto);
            return Ok(booking);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for booking {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create a manual booking for a customer (e.g., phone call)
    /// </summary>
    [HttpPost("manual/booking")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ManualBookingResponseDto>> CreateManualBooking([FromBody] CreateManualBookingDto dto)
    {
        try
        {
            var booking = await _manualBookingService.CreateManualBookingAsync(dto);

            return CreatedAtAction(
                nameof(GetManualBooking),
                new { id = booking.Id },
                booking
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid manual booking request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating manual booking");
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating manual booking");
            return StatusCode(500, new { message = "Ein unerwarteter Fehler ist aufgetreten" });
        }
    }

    /// <summary>
    /// Get a manual booking by ID
    /// </summary>
    [HttpGet("manual/booking/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ManualBookingResponseDto>> GetManualBooking(Guid id)
    {
        var booking = await _manualBookingService.GetManualBookingByIdAsync(id);

        if (booking == null)
        {
            return NotFound(new { message = "Buchung nicht gefunden" });
        }

        return Ok(booking);
    }
}
