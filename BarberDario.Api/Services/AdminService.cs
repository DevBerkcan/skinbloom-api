using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class AdminService
{
    private readonly BarberDarioDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(BarberDarioDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        // Today's bookings
        var todayBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Where(b => b.BookingDate == today)
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        var todayOverview = new TodayOverviewDto(
            today.ToString("yyyy-MM-dd"),
            todayBookings.Count,
            todayBookings.Count(b => b.Status == BookingStatus.Completed),
            todayBookings.Count(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed),
            todayBookings.Count(b => b.Status == BookingStatus.Cancelled),
            todayBookings.Where(b => b.Status == BookingStatus.Completed).Sum(b => b.Service.Price),
            todayBookings.Select(b => MapToBookingListItem(b)).ToList()
        );

        // Next upcoming booking
        var nextBooking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Where(b =>
                (b.BookingDate > today || (b.BookingDate == today && b.StartTime > TimeOnly.FromDateTime(now))) &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .FirstOrDefaultAsync();

        UpcomingBookingDto? upcomingBooking = null;
        if (nextBooking != null)
        {
            var bookingDateTime = nextBooking.BookingDate.ToDateTime(nextBooking.StartTime);
            var minutesUntil = (int)(bookingDateTime - now).TotalMinutes;

            upcomingBooking = new UpcomingBookingDto(
                nextBooking.Id,
                nextBooking.BookingNumber,
                nextBooking.Service.Name,
                $"{nextBooking.Customer.FirstName} {nextBooking.Customer.LastName}",
                nextBooking.BookingDate.ToString("yyyy-MM-dd"),
                nextBooking.StartTime.ToString("HH:mm"),
                nextBooking.EndTime.ToString("HH:mm"),
                minutesUntil
            );
        }

        // Statistics
        var statistics = await GetDashboardStatisticsAsync();

        return new DashboardOverviewDto(todayOverview, upcomingBooking, statistics);
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddDays(-1);

        var thisMonthBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= startOfMonth && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var lastMonthBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= startOfLastMonth && b.CreatedAt <= endOfLastMonth && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var totalCustomers = await _context.Customers.CountAsync();
        var newCustomersThisMonth = await _context.Customers
            .CountAsync(c => c.CreatedAt >= startOfMonth);

        // Load bookings first, then group in memory
        var completedBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed)
            .ToListAsync();

        var popularServices = completedBookings
            .GroupBy(b => new { b.Service.Id, b.Service.Name })
            .Select(g => new PopularServiceDto(
                g.Key.Name,
                g.Count(),
                g.Sum(b => b.Service.Price)
            ))
            .OrderByDescending(s => s.BookingCount)
            .Take(5)
            .ToList();

        var allCompletedBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed)
            .ToListAsync();

        var avgBookingValue = allCompletedBookings.Any()
            ? allCompletedBookings.Average(b => b.Service.Price)
            : 0;

        return new DashboardStatisticsDto(
            thisMonthBookings.Count,
            lastMonthBookings.Count,
            thisMonthBookings.Sum(b => b.Service.Price),
            lastMonthBookings.Sum(b => b.Service.Price),
            totalCustomers,
            newCustomersThisMonth,
            avgBookingValue,
            popularServices
        );
    }

    public async Task<PagedResponseDto<BookingListItemDto>> GetBookingsAsync(BookingFilterDto filter)
    {
        var query = _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
        {
            if (Enum.TryParse<BookingStatus>(filter.Status, true, out var status))
            {
                query = query.Where(b => b.Status == status);
            }
        }

        if (filter.FromDate.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(filter.FromDate.Value);
            query = query.Where(b => b.BookingDate >= fromDate);
        }

        if (filter.ToDate.HasValue)
        {
            var toDate = DateOnly.FromDateTime(filter.ToDate.Value);
            query = query.Where(b => b.BookingDate <= toDate);
        }

        if (filter.ServiceId.HasValue)
        {
            query = query.Where(b => b.ServiceId == filter.ServiceId.Value);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var search = filter.SearchTerm.ToLower();
            query = query.Where(b =>
                b.BookingNumber.ToLower().Contains(search) ||
                b.Customer.FirstName.ToLower().Contains(search) ||
                b.Customer.LastName.ToLower().Contains(search) ||
                b.Customer.Email.ToLower().Contains(search) ||
                b.Customer.Phone.Contains(search)
            );
        }

        var totalCount = await query.CountAsync();

        var bookings = await query
            .OrderByDescending(b => b.BookingDate)
            .ThenByDescending(b => b.StartTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = bookings.Select(MapToBookingListItem).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

        return new PagedResponseDto<BookingListItemDto>(
            items,
            totalCount,
            filter.Page,
            filter.PageSize,
            totalPages
        );
    }

    public async Task<BookingListItemDto> UpdateBookingStatusAsync(Guid bookingId, UpdateBookingStatusDto dto)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new ArgumentException("Buchung nicht gefunden");
        }

        // Parse status
        if (!Enum.TryParse<BookingStatus>(dto.Status, true, out var newStatus))
        {
            throw new ArgumentException($"Ungültiger Status: {dto.Status}");
        }

        var oldStatus = booking.Status;
        booking.Status = newStatus;
        booking.AdminNotes = dto.AdminNotes;
        booking.UpdatedAt = DateTime.UtcNow;

        // Set specific timestamps based on status
        if (newStatus == BookingStatus.Cancelled && !booking.CancelledAt.HasValue)
        {
            booking.CancelledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking {BookingId} status updated from {OldStatus} to {NewStatus} by admin",
            bookingId, oldStatus, newStatus
        );

        return MapToBookingListItem(booking);
    }

    private BookingListItemDto MapToBookingListItem(Booking booking)
    {
        return new BookingListItemDto(
            booking.Id,
            booking.BookingNumber,
            booking.Status.ToString(),
            booking.Service.Name,
            $"{booking.Customer.FirstName} {booking.Customer.LastName}",
            booking.Customer.Email,
            booking.Customer.Phone,
            booking.BookingDate.ToString("yyyy-MM-dd"),
            booking.StartTime.ToString("HH:mm"),
            booking.EndTime.ToString("HH:mm"),
            booking.Service.Price,
            booking.CustomerNotes,
            booking.CreatedAt
        );
    }

    public async Task<TrackingStatisticsDto> GetTrackingStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Default to last 30 days if no date range specified
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        // Get all bookings in date range with tracking data
        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= from && b.CreatedAt <= to)
            .ToListAsync();

        var totalBookings = bookings.Count;
        var bookingsWithTracking = bookings.Count(b =>
            !string.IsNullOrEmpty(b.UtmSource) ||
            !string.IsNullOrEmpty(b.UtmMedium) ||
            !string.IsNullOrEmpty(b.UtmCampaign) ||
            !string.IsNullOrEmpty(b.ReferrerUrl));

        var totalRevenue = bookings.Sum(b => b.Service.Price);
        var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

        // Group by UTM Source
        var utmSources = bookings
            .Where(b => !string.IsNullOrEmpty(b.UtmSource))
            .GroupBy(b => b.UtmSource!)
            .Select(g => new SourceStatisticDto(
                g.Key,
                g.Count(),
                g.Sum(b => b.Service.Price),
                totalBookings > 0 ? (double)g.Count() / totalBookings * 100 : 0
            ))
            .OrderByDescending(s => s.BookingCount)
            .ToList();

        // Group by UTM Medium
        var utmMediums = bookings
            .Where(b => !string.IsNullOrEmpty(b.UtmMedium))
            .GroupBy(b => b.UtmMedium!)
            .Select(g => new SourceStatisticDto(
                g.Key,
                g.Count(),
                g.Sum(b => b.Service.Price),
                totalBookings > 0 ? (double)g.Count() / totalBookings * 100 : 0
            ))
            .OrderByDescending(s => s.BookingCount)
            .ToList();

        // Group by UTM Campaign
        var utmCampaigns = bookings
            .Where(b => !string.IsNullOrEmpty(b.UtmCampaign))
            .GroupBy(b => b.UtmCampaign!)
            .Select(g => new SourceStatisticDto(
                g.Key,
                g.Count(),
                g.Sum(b => b.Service.Price),
                totalBookings > 0 ? (double)g.Count() / totalBookings * 100 : 0
            ))
            .OrderByDescending(s => s.BookingCount)
            .ToList();

        // Top Referrers
        var topReferrers = bookings
            .Where(b => !string.IsNullOrEmpty(b.ReferrerUrl))
            .GroupBy(b => b.ReferrerUrl!)
            .Select(g => new ReferrerStatisticDto(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(10)
            .ToList();

        return new TrackingStatisticsDto(
            totalBookings,
            bookingsWithTracking,
            utmSources,
            utmMediums,
            utmCampaigns,
            topReferrers,
            totalRevenue,
            averageBookingValue
        );
    }

    // Blocked Time Slot Management
    public async Task<List<BlockedTimeSlotDto>> GetBlockedSlotsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.BlockedTimeSlots.AsQueryable();

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(b => b.BlockDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(b => b.BlockDate <= to);
        }

        var blockedSlots = await query
            .OrderBy(b => b.BlockDate)
            .ThenBy(b => b.StartTime)
            .ToListAsync();

        return blockedSlots.Select(b => new BlockedTimeSlotDto(
            b.Id,
            b.BlockDate.ToString("yyyy-MM-dd"),
            b.StartTime.ToString("HH:mm"),
            b.EndTime.ToString("HH:mm"),
            b.Reason,
            b.CreatedAt
        )).ToList();
    }

    public async Task<BlockedTimeSlotDto> CreateBlockedSlotAsync(CreateBlockedSlotDto dto)
    {
        // Parse and validate date/time
        if (!DateOnly.TryParseExact(dto.BlockDate, "yyyy-MM-dd", out var blockDate))
        {
            throw new ArgumentException("Ungültiges Datum. Format: YYYY-MM-DD");
        }

        if (!TimeOnly.TryParseExact(dto.StartTime, "HH:mm", out var startTime))
        {
            throw new ArgumentException("Ungültige Startzeit. Format: HH:mm");
        }

        if (!TimeOnly.TryParseExact(dto.EndTime, "HH:mm", out var endTime))
        {
            throw new ArgumentException("Ungültige Endzeit. Format: HH:mm");
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException("Endzeit muss nach der Startzeit liegen");
        }

        // Check for overlapping blocked slots
        var hasOverlap = await _context.BlockedTimeSlots
            .AnyAsync(b =>
                b.BlockDate == blockDate &&
                ((b.StartTime < endTime && b.EndTime > startTime) ||
                 (startTime < b.EndTime && endTime > b.StartTime)));

        if (hasOverlap)
        {
            throw new InvalidOperationException("Es existiert bereits ein blockierter Zeitslot in diesem Zeitraum");
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

        _logger.LogInformation(
            "Created blocked time slot for {Date} from {StartTime} to {EndTime}",
            blockDate, startTime, endTime
        );

        return new BlockedTimeSlotDto(
            blockedSlot.Id,
            blockedSlot.BlockDate.ToString("yyyy-MM-dd"),
            blockedSlot.StartTime.ToString("HH:mm"),
            blockedSlot.EndTime.ToString("HH:mm"),
            blockedSlot.Reason,
            blockedSlot.CreatedAt
        );
    }

    public async Task DeleteBlockedSlotAsync(Guid id)
    {
        var blockedSlot = await _context.BlockedTimeSlots.FindAsync(id);

        if (blockedSlot == null)
        {
            throw new ArgumentException("Blockierter Zeitslot nicht gefunden");
        }

        _context.BlockedTimeSlots.Remove(blockedSlot);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted blocked time slot {Id} for {Date} from {StartTime} to {EndTime}",
            id, blockedSlot.BlockDate, blockedSlot.StartTime, blockedSlot.EndTime
        );
    }
}
