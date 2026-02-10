using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using BarberDario.Api.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly ILogger<BookingsController> _logger;
    private readonly EmailService _emailService;

    public BookingsController(BookingService bookingService, ILogger<BookingsController> logger, EmailService emailService)
    {
        _bookingService = bookingService;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        // Validierung
        var validator = new CreateBookingValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(new { errors });
        }

        try
        {
            var booking = await _bookingService.CreateBookingAsync(dto);
            _logger.LogInformation("Booking created successfully: {BookingId}", booking.Id);

            return CreatedAtAction(
                nameof(GetBooking),
                new { id = booking.Id },
                booking
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid booking request");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Booking conflict or validation error");
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get booking by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
        {
            return NotFound(new { message = "Buchung nicht gefunden" });
        }

        return Ok(booking);
    }

    /// <summary>
    /// Get bookings by customer email
    /// </summary>
    [HttpGet("by-email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingResponseDto>>> GetBookingsByEmail(string email)
    {
        var bookings = await _bookingService.GetBookingsByEmailAsync(email);
        return Ok(bookings);
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CancelBookingResponseDto>> CancelBooking(
        Guid id,
        [FromBody] CancelBookingDto? dto = null)
    {
        dto ??= new CancelBookingDto(null, true);

        try
        {
            var result = await _bookingService.CancelBookingAsync(id, dto);
            _logger.LogInformation("Booking cancelled: {BookingId}", id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Booking not found: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel booking: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("confirm/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmBooking(string token)
    {
        var (bookingId, action) = _emailService.DecodeToken(token);

        if (bookingId == Guid.Empty || action != "confirm")
        {
            return BadRequest(new { message = "Invalid confirmation link" });
        }

        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        try
        {
            await _bookingService.ConfirmBookingAsync(bookingId);
            return Ok(new { message = "Booking confirmed successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cancel/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBookingByToken(string token)
    {
        var (bookingId, action) = _emailService.DecodeToken(token);

        if (bookingId == Guid.Empty || action != "cancel")
        {
            return BadRequest(new { message = "Invalid cancellation link" });
        }

        var booking = await _bookingService.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return NotFound(new { message = "Booking not found" });
        }

        try
        {
            var dto = new CancelBookingDto("Cancelled by customer via email link", true);
            await _bookingService.CancelBookingAsync(bookingId, dto);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
