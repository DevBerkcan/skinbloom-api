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

    public async Task<List<BlockedTimeSlotDto>> CreateBlockedDateRangeAsync(CreateBlockedDateRangeDto dto)
    {
        // Parse dates
        if (!DateOnly.TryParse(dto.FromDate, out var fromDate))
        {
            throw new ArgumentException("Ungültiges Startdatum-Format. Verwende YYYY-MM-DD");
        }

        if (!DateOnly.TryParse(dto.ToDate, out var toDate))
        {
            throw new ArgumentException("Ungültiges Enddatum-Format. Verwende YYYY-MM-DD");
        }

        // Parse times
        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
        {
            throw new ArgumentException("Ungültiges Startzeit-Format. Verwende HH:mm");
        }

        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
        {
            throw new ArgumentException("Ungültiges Endzeit-Format. Verwende HH:mm");
        }

        // Validate date range
        if (fromDate > toDate)
        {
            throw new ArgumentException("Startdatum muss vor Enddatum liegen");
        }

        // Validate time range
        if (startTime >= endTime)
        {
            throw new ArgumentException("Startzeit muss vor Endzeit liegen");
        }

        // Validate dates are not in the past
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (fromDate < today)
        {
            throw new ArgumentException("Startdatum kann nicht in der Vergangenheit liegen");
        }

        var createdSlots = new List<BlockedTimeSlotDto>();
        var currentDate = fromDate;

        // Loop through each day in the range
        while (currentDate <= toDate)
        {
            // Check for overlapping blocked slots on this date
            var overlapping = await _context.BlockedTimeSlots
                .AnyAsync(b => b.BlockDate == currentDate &&
                              ((b.StartTime <= startTime && b.EndTime > startTime) ||
                               (b.StartTime < endTime && b.EndTime >= endTime) ||
                               (b.StartTime >= startTime && b.EndTime <= endTime)));

            if (overlapping)
            {
                throw new InvalidOperationException(
                    $"Zeitslot am {currentDate:dd.MM.yyyy} überschneidet sich mit einem bestehenden blockierten Zeitraum");
            }

            // Check for existing bookings on this date
            var hasBookings = await _context.Bookings
                .AnyAsync(b => b.BookingDate == currentDate &&
                              b.Status != BookingStatus.Cancelled &&
                              ((b.StartTime < endTime && b.EndTime > startTime)));

            if (hasBookings)
            {
                throw new InvalidOperationException(
                    $"Kann Zeitslot am {currentDate:dd.MM.yyyy} nicht blockieren - es existieren bereits Buchungen in diesem Zeitraum");
            }

            // Create blocked slot for this date
            var blockedSlot = new BlockedTimeSlot
            {
                Id = Guid.NewGuid(),
                BlockDate = currentDate,
                StartTime = startTime,
                EndTime = endTime,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow
            };

            _context.BlockedTimeSlots.Add(blockedSlot);
            createdSlots.Add(new BlockedTimeSlotDto(
                blockedSlot.Id,
                blockedSlot.BlockDate,
                blockedSlot.StartTime,
                blockedSlot.EndTime,
                blockedSlot.Reason,
                blockedSlot.CreatedAt
            ));

            currentDate = currentDate.AddDays(1);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Blocked time slots created in range: {FromDate} - {ToDate}, Count: {Count}",
            fromDate, toDate, createdSlots.Count);

        return createdSlots;
    }

    public async Task<BlockedTimeSlotDto> CreateBlockedTimeSlotAsync(CreateBlockedTimeSlotDto dto)
    {
        // Parse strings to DateOnly and TimeOnly
        if (!DateOnly.TryParse(dto.BlockDate, out var blockDate))
        {
            throw new ArgumentException("Ungültiges Datumsformat. Verwende YYYY-MM-DD");
        }

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
        {
            throw new ArgumentException("Ungültiges Zeitformat. Verwende HH:mm");
        }

        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
        {
            throw new ArgumentException("Ungültiges Zeitformat. Verwende HH:mm");
        }

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

        _logger.LogInformation("Blocked time slot created: {Id} for {Date} {StartTime}-{EndTime}",
            blockedSlot.Id, blockDate, startTime, endTime);

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

        // Parse strings to DateOnly and TimeOnly
        if (!DateOnly.TryParse(dto.BlockDate, out var blockDate))
        {
            throw new ArgumentException("Ungültiges Datumsformat. Verwende YYYY-MM-DD");
        }

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
        {
            throw new ArgumentException("Ungültiges Zeitformat. Verwende HH:mm");
        }

        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
        {
            throw new ArgumentException("Ungültiges Zeitformat. Verwende HH:mm");
        }

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

        _logger.LogInformation("Blocked time slot updated: {Id} for {Date} {StartTime}-{EndTime}",
            id, blockDate, startTime, endTime);

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