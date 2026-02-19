using BarberDario.Api.Data;
using BarberDario.Api.Data.Entities;
using BarberDario.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BarberDario.Api.Services;

public class AdminService
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(SkinbloomDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var todayBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Where(b => b.BookingDate == today)
            .OrderBy(b => b.StartTime)
            .Select(b => new
            {
                Booking = b,
                CustomerName = b.Customer != null
                    ? $"{b.Customer.FirstName ?? ""} {b.Customer.LastName ?? ""}".Trim()
                    : "Unknown Customer",
                CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                CustomerPhone = b.Customer != null ? b.Customer.Phone : null,
                ServiceName = b.Service != null ? b.Service.Name : "Unknown Service",
                ServicePrice = b.Service != null ? b.Service.Price : 0
            })
            .ToListAsync();

        var todayOverview = new TodayOverviewDto(
            today.ToString("yyyy-MM-dd"),
            todayBookings.Count,
            todayBookings.Count(b => b.Booking.Status == BookingStatus.Completed),
            todayBookings.Count(b => b.Booking.Status == BookingStatus.Pending || b.Booking.Status == BookingStatus.Confirmed),
            todayBookings.Count(b => b.Booking.Status == BookingStatus.Cancelled),
            todayBookings.Where(b => b.Booking.Status == BookingStatus.Completed).Sum(b => b.ServicePrice),
            todayBookings.Select(b => new BookingListItemDto(
                b.Booking.Id,
                Booking.GenerateBookingNumber(b.Booking.BookingDate, b.Booking.Id),
                b.Booking.Status.ToString(),
                b.ServiceName,
                b.CustomerName,
                b.CustomerEmail,
                b.CustomerPhone,
                b.Booking.BookingDate.ToString("yyyy-MM-dd"),
                b.Booking.StartTime.ToString("HH:mm"),
                b.Booking.EndTime.ToString("HH:mm"),
                b.ServicePrice,
                b.Booking.CustomerNotes,
                b.Booking.CreatedAt
            )).ToList()
        );

        var nextBooking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Service)
            .Where(b =>
                (b.BookingDate > today || (b.BookingDate == today && b.StartTime > TimeOnly.FromDateTime(now))) &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .Select(b => new
            {
                b.Id,
                b.BookingDate,
                b.StartTime,
                b.EndTime,
                ServiceName = b.Service != null ? b.Service.Name : "Unknown Service",
                CustomerName = b.Customer != null
                    ? $"{b.Customer.FirstName ?? ""} {b.Customer.LastName ?? ""}".Trim()
                    : "Unknown Customer"
            })
            .FirstOrDefaultAsync();

        UpcomingBookingDto? upcomingBooking = null;
        if (nextBooking != null)
        {
            var bookingDateTime = nextBooking.BookingDate.ToDateTime(nextBooking.StartTime);
            var minutesUntil = (int)(bookingDateTime - now).TotalMinutes;

            upcomingBooking = new UpcomingBookingDto(
                nextBooking.Id,
                Booking.GenerateBookingNumber(nextBooking.BookingDate, nextBooking.Id),
                nextBooking.ServiceName,
                nextBooking.CustomerName,
                nextBooking.BookingDate.ToString("yyyy-MM-dd"),
                nextBooking.StartTime.ToString("HH:mm"),
                nextBooking.EndTime.ToString("HH:mm"),
                minutesUntil
            );
        }

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
            .Select(b => new { b.Status, Price = b.Service != null ? b.Service.Price : 0 })
            .ToListAsync();

        var lastMonthBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= startOfLastMonth && b.CreatedAt <= endOfLastMonth && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.Status, Price = b.Service != null ? b.Service.Price : 0 })
            .ToListAsync();

        var totalCustomers = await _context.Customers.CountAsync();
        var newCustomersThisMonth = await _context.Customers
            .CountAsync(c => c.CreatedAt >= startOfMonth);

        var completedBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => (b.Status == BookingStatus.Completed || b.Status == BookingStatus.Confirmed) && b.Service != null)
            .Select(b => new
            {
                ServiceId = b.Service.Id,
                ServiceName = b.Service.Name,
                Price = b.Service.Price
            })
            .ToListAsync();

        var popularServices = completedBookings
            .GroupBy(b => new { b.ServiceId, b.ServiceName })
            .Select(g => new PopularServiceDto(
                g.Key.ServiceName ?? "Unknown",
                g.Count(),
                g.Sum(b => b.Price)
            ))
            .OrderByDescending(s => s.BookingCount)
            .Take(5)
            .ToList();

        var allCompletedBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Status == BookingStatus.Completed && b.Service != null)
            .Select(b => b.Service.Price)
            .ToListAsync();

        var avgBookingValue = allCompletedBookings.Any()
            ? allCompletedBookings.Average()
            : 0;

        return new DashboardStatisticsDto(
            thisMonthBookings.Count,
            lastMonthBookings.Count,
            thisMonthBookings.Sum(b => b.Price),
            lastMonthBookings.Sum(b => b.Price),
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
            .Select(b => new
            {
                Booking = b,
                CustomerName = b.Customer != null
                    ? $"{b.Customer.FirstName ?? ""} {b.Customer.LastName ?? ""}".Trim()
                    : "Unknown Customer",
                CustomerEmail = b.Customer != null ? b.Customer.Email : null,
                CustomerPhone = b.Customer != null ? b.Customer.Phone : null,
                ServiceName = b.Service != null ? b.Service.Name : "Unknown Service",
                ServicePrice = b.Service != null ? b.Service.Price : 0
            })
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Status))
        {
            if (Enum.TryParse<BookingStatus>(filter.Status, true, out var status))
                query = query.Where(x => x.Booking.Status == status);
        }

        if (filter.FromDate.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(filter.FromDate.Value);
            query = query.Where(x => x.Booking.BookingDate >= fromDate);
        }

        if (filter.ToDate.HasValue)
        {
            var toDate = DateOnly.FromDateTime(filter.ToDate.Value);
            query = query.Where(x => x.Booking.BookingDate <= toDate);
        }

        if (filter.ServiceId.HasValue)
            query = query.Where(x => x.Booking.ServiceId == filter.ServiceId.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var search = filter.SearchTerm.ToLower();
            query = query.Where(x =>
                x.CustomerName.ToLower().Contains(search) ||
                (x.CustomerEmail ?? "").ToLower().Contains(search) ||
                (x.CustomerPhone ?? "").Contains(search) ||
                x.ServiceName.ToLower().Contains(search)
            );
        }

        var totalCount = await query.CountAsync();

        var bookings = await query
            .OrderByDescending(x => x.Booking.BookingDate)
            .ThenByDescending(x => x.Booking.StartTime)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = bookings.Select(x => new BookingListItemDto(
            x.Booking.Id,
            Booking.GenerateBookingNumber(x.Booking.BookingDate, x.Booking.Id),
            x.Booking.Status.ToString(),
            x.ServiceName,
            x.CustomerName,
            x.CustomerEmail,
            x.CustomerPhone,
            x.Booking.BookingDate.ToString("yyyy-MM-dd"),
            x.Booking.StartTime.ToString("HH:mm"),
            x.Booking.EndTime.ToString("HH:mm"),
            x.ServicePrice,
            x.Booking.CustomerNotes,
            x.Booking.CreatedAt
        )).ToList();

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
            throw new ArgumentException("Buchung nicht gefunden");

        if (!Enum.TryParse<BookingStatus>(dto.Status, true, out var newStatus))
            throw new ArgumentException($"Ungültiger Status: {dto.Status}");

        var oldStatus = booking.Status;
        booking.Status = newStatus;
        booking.AdminNotes = dto.AdminNotes;
        booking.UpdatedAt = DateTime.UtcNow;

        if (newStatus == BookingStatus.Cancelled && !booking.CancelledAt.HasValue)
            booking.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Booking {BookingId} status updated from {OldStatus} to {NewStatus} by admin",
            bookingId, oldStatus, newStatus
        );

        var customerName = booking.Customer != null
            ? $"{booking.Customer.FirstName ?? ""} {booking.Customer.LastName ?? ""}".Trim()
            : "Unknown Customer";

        if (string.IsNullOrWhiteSpace(customerName))
            customerName = "Unknown Customer";

        return new BookingListItemDto(
            booking.Id,
            Booking.GenerateBookingNumber(booking.BookingDate, booking.Id),
            booking.Status.ToString(),
            booking.Service?.Name ?? "Unknown Service",
            customerName,
            booking.Customer?.Email,
            booking.Customer?.Phone,
            booking.BookingDate.ToString("yyyy-MM-dd"),
            booking.StartTime.ToString("HH:mm"),
            booking.EndTime.ToString("HH:mm"),
            booking.Service?.Price ?? 0,
            booking.CustomerNotes,
            booking.CreatedAt
        );
    }
}