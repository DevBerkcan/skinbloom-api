using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(AvailabilityService availabilityService, ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Get available time slots for a service on a specific date
    /// </summary>
    /// <param name="serviceId">Service ID</param>
    /// <param name="date">Date in format YYYY-MM-DD</param>
    [HttpGet("{serviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailabilityResponseDto>> GetAvailability(
        Guid serviceId,
        [FromQuery] string date)
    {
        if (string.IsNullOrEmpty(date))
        {
            return BadRequest(new { message = "Date parameter is required" });
        }

        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { message = "Invalid date format. Use YYYY-MM-DD" });
        }

        // Prüfe ob Datum in der Vergangenheit liegt
        if (parsedDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "Cannot book appointments in the past" });
        }

        try
        {
            var availability = await _availabilityService.GetAvailableTimeSlotsAsync(serviceId, parsedDate);
            _logger.LogInformation("Retrieved availability for service {ServiceId} on {Date}", serviceId, date);
            return Ok(availability);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Service {ServiceId} not found or inactive", serviceId);
            return NotFound(new { message = ex.Message });
        }
    }
}
