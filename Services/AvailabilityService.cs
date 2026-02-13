using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using BarberDario.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class AvailabilityService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(SkinbloomDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AvailabilityResponseDto> GetAvailableTimeSlotsAsync(Guid serviceId, DateOnly date)
    {
        // 1. Service abrufen
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null || !service.IsActive)
        {
            throw new ArgumentException("Service not found or inactive", nameof(serviceId));
        }

        // 2. Öffnungszeiten für den Wochentag abrufen
        var dayOfWeek = date.DayOfWeek;
        var businessHours = await _context.BusinessHours
            .FirstOrDefaultAsync(bh => bh.DayOfWeek == dayOfWeek);

        if (businessHours == null || !businessHours.IsOpen)
        {
            return new AvailabilityResponseDto(
                date.ToString("yyyy-MM-dd"),
                serviceId,
                service.DurationMinutes,
                new List<TimeSlotDto>()
            );
        }

        // 3. Buchungsintervall aus Settings holen
        var intervalSetting = await _context.Settings.FindAsync("BOOKING_INTERVAL_MINUTES");
        var intervalMinutes = int.Parse(intervalSetting?.Value ?? "15");

        // 4. Alle Zeitslots generieren
        var timeSlots = GenerateTimeSlots(
            businessHours.OpenTime,
            businessHours.CloseTime,
            service.DurationMinutes,
            intervalMinutes,
            businessHours.BreakStartTime,
            businessHours.BreakEndTime
        );

        // 5. FIX: Gebuchte Zeitslots abrufen - ALLE Services, nicht nur das angefragte!
        //    Ein Barbier kann nur einen Kunden gleichzeitig bedienen, egal welcher Service
        var bookedSlots = await _context.Bookings
            .Where(b => b.BookingDate == date &&
                       b.Status != BookingStatus.Cancelled) // ❌ KEINE ServiceId Filterung!
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync();

        // 6. Gesperrte Zeitslots abrufen
        var blockedSlots = await _context.BlockedTimeSlots
            .Where(bs => bs.BlockDate == date)
            .Select(bs => new { bs.StartTime, bs.EndTime })
            .ToListAsync();

        // 7. Verfügbarkeit prüfen
        var availableSlots = timeSlots.Select(slot =>
        {
            // Prüfe ob Slot von IRGENDEINER Buchung überlappt wird (alle Services!)
            var isBooked = bookedSlots.Any(b =>
                slot.Start < b.EndTime && slot.End > b.StartTime);

            // Prüfe ob Slot von einem Blocked-Slot überlappt wird
            var isBlocked = blockedSlots.Any(b =>
                slot.Start < b.EndTime && slot.End > b.StartTime);

            return new TimeSlotDto(
                slot.Start.ToString("HH:mm"),
                slot.End.ToString("HH:mm"),
                !isBooked && !isBlocked
            );
        }).ToList();

        return new AvailabilityResponseDto(
            date.ToString("yyyy-MM-dd"),
            serviceId,
            service.DurationMinutes,
            availableSlots
        );
    }

    private List<(TimeOnly Start, TimeOnly End)> GenerateTimeSlots(
        TimeOnly openTime,
        TimeOnly closeTime,
        int serviceDuration,
        int intervalMinutes,
        TimeOnly? breakStart,
        TimeOnly? breakEnd)
    {
        var slots = new List<(TimeOnly Start, TimeOnly End)>();
        var currentTime = openTime;

        while (currentTime.AddMinutes(serviceDuration) <= closeTime)
        {
            var slotEnd = currentTime.AddMinutes(serviceDuration);

            // Prüfe ob Slot in Pausenzeit fällt
            if (breakStart.HasValue && breakEnd.HasValue)
            {
                var inBreak = currentTime < breakEnd.Value && slotEnd > breakStart.Value;
                if (inBreak)
                {
                    currentTime = currentTime.AddMinutes(intervalMinutes);
                    continue;
                }
            }

            slots.Add((currentTime, slotEnd));
            currentTime = currentTime.AddMinutes(intervalMinutes);
        }

        return slots;
    }

    public async Task<bool> IsTimeSlotAvailableAsync(Guid serviceId, DateOnly date, TimeOnly startTime)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null || !service.IsActive)
            return false;

        var endTime = startTime.AddMinutes(service.DurationMinutes);

        // Check for any non-cancelled booking at this time
        var existingBooking = await _context.Bookings
            .AnyAsync(b => b.BookingDate == date &&
                          b.ServiceId == serviceId &&
                          b.Status != BookingStatus.Cancelled &&
                          b.StartTime < endTime &&
                          b.EndTime > startTime);

        if (existingBooking)
            return false;

        // Check for blocked slots
        var isBlocked = await _context.BlockedTimeSlots
            .AnyAsync(bs => bs.BlockDate == date &&
                           bs.StartTime < endTime &&
                           bs.EndTime > startTime);

        return !isBlocked;
    }
}