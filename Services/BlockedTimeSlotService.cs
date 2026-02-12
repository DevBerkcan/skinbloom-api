using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class BlockedTimeSlotService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<BlockedTimeSlotService> _logger;

    public BlockedTimeSlotService(
        SkinbloomDbContext context,
        ILogger<BlockedTimeSlotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<BlockedTimeSlotDto>> GetBlockedTimeSlotsAsync(
        DateOnly startDate, DateOnly endDate)
    {
        var blockedSlots = await _context.BlockedTimeSlots
            .Where(b => b.BlockDate >= startDate && b.BlockDate <= endDate)
            .OrderBy(b => b.BlockDate)
            .ThenBy(b => b.StartTime)
            .Select(b => new BlockedTimeSlotDto(
                b.Id,
                b.BlockDate,
                b.StartTime,
                b.EndTime,
                b.Reason,
                b.CreatedAt
            ))
            .ToListAsync();

        return blockedSlots;
    }

    public async Task<BlockedTimeSlotDto?> GetBlockedTimeSlotByIdAsync(Guid id)
    {
        var blockedSlot = await _context.BlockedTimeSlots
            .Where(b => b.Id == id)
            .Select(b => new BlockedTimeSlotDto(
                b.Id,
                b.BlockDate,
                b.StartTime,
                b.EndTime,
                b.Reason,
                b.CreatedAt
            ))
            .FirstOrDefaultAsync();

        return blockedSlot;
    }

    public async Task<BlockedTimeSlotDto> CreateBlockedTimeSlotAsync(CreateBlockedTimeSlotDto dto)
    {
        // Convert from DTO to DateOnly/TimeOnly
        var blockDate = new DateOnly(
            dto.BlockDate.Year,
            dto.BlockDate.Month,
            dto.BlockDate.Day
        );

        var startTime = new TimeOnly(
            dto.StartTime.Hour,
            dto.StartTime.Minute
        );

        var endTime = new TimeOnly(
            dto.EndTime.Hour,
            dto.EndTime.Minute
        );

        // Validate time range
        if (startTime >= endTime)
        {
            throw new ArgumentException("Startzeit muss vor Endzeit liegen");
        }

        // Validate date is not in the past
        if (blockDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new ArgumentException("Datum kann nicht in der Vergangenheit liegen");
        }

        // Check for overlapping blocked slots
        var overlapping = await _context.BlockedTimeSlots
            .AnyAsync(b => b.BlockDate == blockDate &&
                          ((b.StartTime <= startTime && b.EndTime > startTime) ||
                           (b.StartTime < endTime && b.EndTime >= endTime) ||
                           (b.StartTime >= startTime && b.EndTime <= endTime)));

        if (overlapping)
        {
            throw new InvalidOperationException("Zeitslot überschneidet sich mit einem bestehenden blockierten Zeitraum");
        }

        // Check for existing bookings in this time slot
        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.BookingDate == blockDate &&
                          b.Status != BookingStatus.Cancelled &&
                          ((b.StartTime < endTime && b.EndTime > startTime)));

        if (hasBookings)
        {
            throw new InvalidOperationException("Kann Zeitslot nicht blockieren - es existieren bereits Buchungen in diesem Zeitraum");
        }

        var blockedSlot = new BlockedTimeSlot
        {
            Id = Guid.NewGuid(),
            BlockDate = blockDate,
            StartTime = startTime,
            EndTime = endTime,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        _context.BlockedTimeSlots.Add(blockedSlot);
        await _context.SaveChangesAsync();

        return new BlockedTimeSlotDto(
            blockedSlot.Id,
            blockedSlot.BlockDate,
            blockedSlot.StartTime,
            blockedSlot.EndTime,
            blockedSlot.Reason,
            blockedSlot.CreatedAt
        );
    }


    public async Task<BlockedTimeSlotDto> UpdateBlockedTimeSlotAsync(
        Guid id, UpdateBlockedTimeSlotDto dto)
    {
        var blockedSlot = await _context.BlockedTimeSlots.FindAsync(id);
        if (blockedSlot == null)
        {
            throw new ArgumentException("Blockierter Zeitslot nicht gefunden");
        }

        // Convert from DTO to DateOnly/TimeOnly
        var blockDate = new DateOnly(
            dto.BlockDate.Year,
            dto.BlockDate.Month,
            dto.BlockDate.Day
        );

        var startTime = new TimeOnly(
            dto.StartTime.Hour,
            dto.StartTime.Minute
        );

        var endTime = new TimeOnly(
            dto.EndTime.Hour,
            dto.EndTime.Minute
        );

        // Validate time range
        if (startTime >= endTime)
        {
            throw new InvalidOperationException("Startzeit muss vor Endzeit liegen");
        }

        // Validate date is not in the past
        if (blockDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new ArgumentException("Datum kann nicht in der Vergangenheit liegen");
        }

        // Check for overlapping blocked slots (excluding current)
        var overlapping = await _context.BlockedTimeSlots
            .AnyAsync(b => b.Id != id &&
                          b.BlockDate == blockDate &&
                          ((b.StartTime <= startTime && b.EndTime > startTime) ||
                           (b.StartTime < endTime && b.EndTime >= endTime) ||
                           (b.StartTime >= startTime && b.EndTime <= endTime)));

        if (overlapping)
        {
            throw new InvalidOperationException("Zeitslot überschneidet sich mit einem bestehenden blockierten Zeitraum");
        }

        // Check for existing bookings in this time slot
        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.BookingDate == blockDate &&
                          b.Status != BookingStatus.Cancelled &&
                          ((b.StartTime < endTime && b.EndTime > startTime)));

        if (hasBookings)
        {
            throw new InvalidOperationException("Kann Zeitslot nicht blockieren - es existieren bereits Buchungen in diesem Zeitraum");
        }

        blockedSlot.BlockDate = blockDate;
        blockedSlot.StartTime = startTime;
        blockedSlot.EndTime = endTime;
        blockedSlot.Reason = dto.Reason;

        await _context.SaveChangesAsync();

        return new BlockedTimeSlotDto(
            blockedSlot.Id,
            blockedSlot.BlockDate,
            blockedSlot.StartTime,
            blockedSlot.EndTime,
            blockedSlot.Reason,
            blockedSlot.CreatedAt
        );
    }

    public async Task DeleteBlockedTimeSlotAsync(Guid id)
    {
        var blockedSlot = await _context.BlockedTimeSlots.FindAsync(id);
        if (blockedSlot == null)
        {
            throw new ArgumentException("Blockierter Zeitslot nicht gefunden");
        }

        _context.BlockedTimeSlots.Remove(blockedSlot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Blocked time slot deleted: {Id}", id);
    }
}