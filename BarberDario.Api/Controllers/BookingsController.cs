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

    public BookingsController(BookingService bookingService, ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
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
    /// Confirm a booking (change status from Pending to Confirmed)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingResponseDto>> ConfirmBooking(
        Guid id,
        [FromQuery] bool notifyCustomer = true)
    {
        try
        {
            var result = await _bookingService.ConfirmBookingAsync(id, notifyCustomer);
            _logger.LogInformation("Booking confirmed: {BookingId}", id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Booking not found: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot confirm booking: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update booking status (Admin only)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingResponseDto>> UpdateBookingStatus(
        Guid id,
        [FromBody] UpdateBookingStatusDto dto)
    {
        try
        {
            var result = await _bookingService.UpdateBookingStatusAsync(id, dto.Status, dto.AdminNotes);
            _logger.LogInformation("Booking status updated: {BookingId} to {Status}", id, dto.Status);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Booking not found: {BookingId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update booking status: {BookingId}", id);
            return BadRequest(new { message = ex.Message });
        }
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
}
