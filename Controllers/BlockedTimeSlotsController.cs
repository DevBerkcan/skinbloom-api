// BarberDario.Api.Controllers/BlockedTimeSlotsController.cs
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlockedTimeSlotsController : ControllerBase
{
    private readonly BlockedTimeSlotService _blockedTimeSlotService;
    private readonly ILogger<BlockedTimeSlotsController> _logger;

    public BlockedTimeSlotsController(
        BlockedTimeSlotService blockedTimeSlotService,
        ILogger<BlockedTimeSlotsController> logger)
    {
        _blockedTimeSlotService = blockedTimeSlotService;
        _logger = logger;
    }

    /// <summary>
    /// Get all blocked time slots
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BlockedTimeSlotDto>>> GetBlockedTimeSlots(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var blockedSlots = await _blockedTimeSlotService.GetBlockedTimeSlotsAsync(
            DateOnly.FromDateTime(startDate ?? DateTime.MinValue),
            DateOnly.FromDateTime(endDate ?? DateTime.MaxValue));
        return Ok(blockedSlots);
    }

    /// <summary>
    /// Get blocked time slot by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlockedTimeSlotDto>> GetBlockedTimeSlot(Guid id)
    {
        var blockedSlot = await _blockedTimeSlotService.GetBlockedTimeSlotByIdAsync(id);
        if (blockedSlot == null)
        {
            return NotFound(new { message = "Blocked time slot not found" });
        }
        return Ok(blockedSlot);
    }

    /// <summary>
    /// Create a new blocked time slot
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BlockedTimeSlotDto>> CreateBlockedTimeSlot(
        [FromBody] CreateBlockedTimeSlotDto dto)
    {
        try
        {
            var blockedSlot = await _blockedTimeSlotService.CreateBlockedTimeSlotAsync(dto);
            _logger.LogInformation("Blocked time slot created: {Id}", blockedSlot.Id);

            return CreatedAtAction(
                nameof(GetBlockedTimeSlot),
                new { id = blockedSlot.Id },
                blockedSlot
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a blocked time slot
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlockedTimeSlotDto>> UpdateBlockedTimeSlot(
        Guid id,
        [FromBody] UpdateBlockedTimeSlotDto dto)
    {
        try
        {
            var blockedSlot = await _blockedTimeSlotService.UpdateBlockedTimeSlotAsync(id, dto);
            return Ok(blockedSlot);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a blocked time slot
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBlockedTimeSlot(Guid id)
    {
        try
        {
            await _blockedTimeSlotService.DeleteBlockedTimeSlotAsync(id);
            _logger.LogInformation("Blocked time slot deleted: {Id}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}