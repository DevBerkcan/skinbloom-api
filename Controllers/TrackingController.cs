using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skinbloom.Api.Data.Entities;

namespace BarberDario.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tracking")]
public class TrackingController : ControllerBase
{
    private readonly SkinbloomDbContext _context;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(SkinbloomDbContext context, ILogger<TrackingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Track a link click
    /// </summary>
    [HttpPost("click")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackLinkClick([FromBody] TrackLinkClickDto dto)
    {
        var linkClick = new LinkClick
        {
            Id = Guid.NewGuid(),
            LinkName = dto.LinkName,
            LinkUrl = dto.LinkUrl,
            SessionId = dto.SessionId,
            ReferrerUrl = Request.Headers.Referer.ToString(),
            ClickedAt = DateTime.UtcNow
        };

        _context.LinkClicks.Add(linkClick);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Link click tracked: {LinkName}", dto.LinkName);
        return Ok();
    }

    /// <summary>
    /// Get simplified tracking statistics
    /// If no date filters are provided, returns ALL TIME statistics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<SimplifiedTrackingStatisticsDto>> GetTrackingStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        // If no dates provided, get ALL data
        var queryFromDate = fromDate ?? DateTime.MinValue;  // All time from beginning
        var queryToDate = toDate ?? DateTime.UtcNow;        // Up to now

        // Total page views
        var totalPageViews = await _context.PageViews
            .Where(p => p.ViewedAt >= queryFromDate && p.ViewedAt <= queryToDate)
            .CountAsync();

        // Total bookings and revenue
        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= queryFromDate && b.CreatedAt <= queryToDate
                        && b.Status != Data.Entities.BookingStatus.Cancelled)
            .ToListAsync();

        var totalBookings = bookings.Count;
        var totalRevenue = bookings.Sum(b => b.Service?.Price ?? 0);

        // Average booking value
        var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

        // Total link clicks
        var totalLinkClicks = await _context.LinkClicks
            .Where(l => l.ClickedAt >= queryFromDate && l.ClickedAt <= queryToDate)
            .CountAsync();

        // Link click statistics grouped by link name
        var linkClicks = await _context.LinkClicks
            .Where(l => l.ClickedAt >= queryFromDate && l.ClickedAt <= queryToDate)
            .GroupBy(l => l.LinkName)
            .Select(g => new
            {
                LinkName = g.Key,
                ClickCount = g.Count()
            })
            .OrderByDescending(x => x.ClickCount)
            .ToListAsync();

        var totalClicksForPercentage = linkClicks.Sum(x => x.ClickCount);

        var linkClickStats = linkClicks.Select(x => new LinkClickStatisticDto
        {
            LinkName = x.LinkName,
            ClickCount = x.ClickCount,
            Percentage = totalClicksForPercentage > 0
                ? Math.Round((double)x.ClickCount / totalClicksForPercentage * 100, 1)
                : 0
        }).ToList();

        var result = new SimplifiedTrackingStatisticsDto
        {
            TotalBookings = totalBookings,
            TotalPageViews = totalPageViews,
            TotalLinkClicks = totalLinkClicks,
            TotalRevenue = totalRevenue,
            AverageBookingValue = Math.Round(averageBookingValue, 2),
            LinkClicks = linkClickStats
        };

        return Ok(result);
    }

    /// <summary>
    /// Get revenue statistics for different time periods
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RevenueStatisticsDto>> GetRevenueStatistics()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);
        var allTimeStart = DateTime.MinValue; // All time

        // Today's bookings
        var todayBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt.Date == today
                        && b.Status != Data.Entities.BookingStatus.Cancelled)
            .ToListAsync();

        // This week's bookings
        var weekBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= weekAgo
                        && b.Status != Data.Entities.BookingStatus.Cancelled)
            .ToListAsync();

        // This month's bookings
        var monthBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.CreatedAt >= monthAgo
                        && b.Status != Data.Entities.BookingStatus.Cancelled)
            .ToListAsync();

        // ALL TIME bookings
        var allTimeBookings = await _context.Bookings
            .Include(b => b.Service)
            .Where(b => b.Status != Data.Entities.BookingStatus.Cancelled)
            .ToListAsync();

        var result = new RevenueStatisticsDto
        {
            TodayRevenue = Math.Round(todayBookings.Sum(b => b.Service?.Price ?? 0), 2),
            TodayBookings = todayBookings.Count,
            WeekRevenue = Math.Round(weekBookings.Sum(b => b.Service?.Price ?? 0), 2),
            WeekBookings = weekBookings.Count,
            MonthRevenue = Math.Round(monthBookings.Sum(b => b.Service?.Price ?? 0), 2),
            MonthBookings = monthBookings.Count,
            AllTimeRevenue = Math.Round(allTimeBookings.Sum(b => b.Service?.Price ?? 0), 2),
            AllTimeBookings = allTimeBookings.Count
        };

        return Ok(result);
    }

    [HttpPost("pageview")]
    public async Task<IActionResult> TrackPageView([FromBody] TrackPageViewDto dto)
    {
        try
        {
            var pageView = new PageView
            {
                Id = Guid.NewGuid(),
                PageUrl = dto.PageUrl ?? "/",
                ReferrerUrl = dto.ReferrerUrl,
                UtmSource = dto.UtmSource,
                UtmMedium = dto.UtmMedium,
                UtmCampaign = dto.UtmCampaign,
                UtmContent = dto.UtmContent,
                UtmTerm = dto.UtmTerm,
                SessionId = dto.SessionId,
                UserAgent = Request.Headers.UserAgent.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ViewedAt = DateTime.UtcNow
            };

            _context.PageViews.Add(pageView);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Page view tracked: {PageUrl} at {Time}", pageView.PageUrl, DateTime.UtcNow);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking page view");
            return Ok(new { success = false, error = ex.Message });
        }
    }
}