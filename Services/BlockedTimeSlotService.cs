// BarberDario.Api.Services/BlockedTimeSlotService.cs
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
        // Validate time range
        if (dto.StartTime >= dto.EndTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        // Check for overlapping blocked slots
        var overlapping = await _context.BlockedTimeSlots
            .AnyAsync(b => b.BlockDate == dto.BlockDate &&
                          ((b.StartTime <= dto.StartTime && b.EndTime > dto.StartTime) ||
                           (b.StartTime < dto.EndTime && b.EndTime >= dto.EndTime) ||
                           (b.StartTime >= dto.StartTime && b.EndTime <= dto.EndTime)));

        if (overlapping)
        {
            throw new InvalidOperationException("Time slot overlaps with existing blocked time");
        }

        // Check for existing bookings in this time slot
        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.BookingDate == dto.BlockDate &&
                          b.Status != BookingStatus.Cancelled &&
                          ((b.StartTime < dto.EndTime && b.EndTime > dto.StartTime)));

        if (hasBookings)
        {
            throw new InvalidOperationException("Cannot block time slot with existing bookings");
        }

        var blockedSlot = new BlockedTimeSlot
        {
            BlockDate = dto.BlockDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
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
            throw new ArgumentException("Blocked time slot not found");
        }

        if (dto.StartTime >= dto.EndTime)
        {
            throw new InvalidOperationException("Start time must be before end time");
        }

        // Check for overlapping blocked slots (excluding current)
        var overlapping = await _context.BlockedTimeSlots
            .AnyAsync(b => b.Id != id &&
                          b.BlockDate == dto.BlockDate &&
                          ((b.StartTime <= dto.StartTime && b.EndTime > dto.StartTime) ||
                           (b.StartTime < dto.EndTime && b.EndTime >= dto.EndTime) ||
                           (b.StartTime >= dto.StartTime && b.EndTime <= dto.EndTime)));

        if (overlapping)
        {
            throw new InvalidOperationException("Time slot overlaps with existing blocked time");
        }

        // Check for existing bookings in this time slot
        var hasBookings = await _context.Bookings
            .AnyAsync(b => b.BookingDate == dto.BlockDate &&
                          b.Status != BookingStatus.Cancelled &&
                          ((b.StartTime < dto.EndTime && b.EndTime > dto.StartTime)));

        if (hasBookings)
        {
            throw new InvalidOperationException("Cannot block time slot with existing bookings");
        }

        blockedSlot.BlockDate = dto.BlockDate;
        blockedSlot.StartTime = dto.StartTime;
        blockedSlot.EndTime = dto.EndTime;
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
            throw new ArgumentException("Blocked time slot not found");
        }

        _context.BlockedTimeSlots.Remove(blockedSlot);
        await _context.SaveChangesAsync();
    }
}