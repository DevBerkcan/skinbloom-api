using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skinbloom.Api.Data.Entities;

namespace BarberDario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Record a page view
    /// </summary>
    [HttpPost("pageview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackPageView([FromBody] TrackPageViewDto dto)
    {
        var pageView = new PageView
        {
            Id = Guid.NewGuid(),
            PageUrl = dto.PageUrl,
            ReferrerUrl = dto.ReferrerUrl,
            UtmSource = dto.UtmSource,
            UtmMedium = dto.UtmMedium,
            UtmCampaign = dto.UtmCampaign,
            UtmContent = dto.UtmContent,
            UtmTerm = dto.UtmTerm,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            SessionId = dto.SessionId,
            ViewedAt = DateTime.UtcNow
        };

        _context.PageViews.Add(pageView);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Page view recorded: {PageUrl}", dto.PageUrl);
        return Ok();
    }

    /// <summary>
    /// Record a conversion
    /// </summary>
    [HttpPost("conversion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackConversion([FromBody] TrackConversionDto dto)
    {
        var conversion = new Conversion
        {
            Id = Guid.NewGuid(),
            BookingId = dto.BookingId,
            ConversionType = dto.ConversionType,
            PageUrl = dto.PageUrl,
            ReferrerUrl = dto.ReferrerUrl,
            UtmSource = dto.UtmSource,
            UtmMedium = dto.UtmMedium,
            UtmCampaign = dto.UtmCampaign,
            UtmContent = dto.UtmContent,
            UtmTerm = dto.UtmTerm,
            Revenue = dto.Revenue,
            ConvertedAt = DateTime.UtcNow
        };

        _context.Conversions.Add(conversion);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Conversion recorded: {Type} - {Revenue}", dto.ConversionType, dto.Revenue);
        return Ok();
    }

    /// <summary>
    /// Get tracking statistics for admin dashboard
    /// </summary>
    [HttpGet("admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrackingStatisticsDto>> GetTrackingStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        // Get total bookings in period
        var totalBookings = await _context.Bookings
            .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
            .CountAsync();

        // Get bookings with tracking data (UTM parameters)
        var bookingsWithTracking = await _context.Conversions
            .Where(c => c.ConvertedAt >= startDate && c.ConvertedAt <= endDate &&
                       c.ConversionType == "booking")
            .Select(c => c.BookingId)
            .Distinct()
            .CountAsync();

        // Get total revenue
        var totalRevenue = await _context.Conversions
            .Where(c => c.ConvertedAt >= startDate && c.ConvertedAt <= endDate &&
                       c.ConversionType == "booking" && c.Revenue.HasValue)
            .SumAsync(c => c.Revenue) ?? 0;

        // Average booking value
        var averageBookingValue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

        // Get UTM Sources
        var utmSources = await _context.Conversions
            .Where(c => c.ConvertedAt >= startDate && c.ConvertedAt <= endDate &&
                       c.ConversionType == "booking" && c.UtmSource != null)
            .GroupBy(c => c.UtmSource)
            .Select(g => new
            {
                Name = g.Key ?? "direct",
                BookingCount = g.Count(),
                Revenue = g.Sum(c => c.Revenue) ?? 0
            })
            .OrderByDescending(x => x.BookingCount)
            .ToListAsync();

        var totalSourceBookings = utmSources.Sum(x => x.BookingCount);

        var sourceStats = utmSources.Select(x => new SourceStatisticDto
        {
            Name = x.Name,
            BookingCount = x.BookingCount,
            Revenue = x.Revenue,
            Percentage = totalSourceBookings > 0 ? (double)x.BookingCount / totalSourceBookings * 100 : 0
        }).ToList();

        // Get UTM Mediums
        var utmMediums = await _context.Conversions
            .Where(c => c.ConvertedAt >= startDate && c.ConvertedAt <= endDate &&
                       c.ConversionType == "booking" && c.UtmMedium != null)
            .GroupBy(c => c.UtmMedium)
            .Select(g => new
            {
                Name = g.Key ?? "unknown",
                BookingCount = g.Count(),
                Revenue = g.Sum(c => c.Revenue) ?? 0
            })
            .OrderByDescending(x => x.BookingCount)
            .ToListAsync();

        var totalMediumBookings = utmMediums.Sum(x => x.BookingCount);

        var mediumStats = utmMediums.Select(x => new SourceStatisticDto
        {
            Name = x.Name,
            BookingCount = x.BookingCount,
            Revenue = x.Revenue,
            Percentage = totalMediumBookings > 0 ? (double)x.BookingCount / totalMediumBookings * 100 : 0
        }).ToList();

        // Get UTM Campaigns
        var utmCampaigns = await _context.Conversions
            .Where(c => c.ConvertedAt >= startDate && c.ConvertedAt <= endDate &&
                       c.ConversionType == "booking" && c.UtmCampaign != null)
            .GroupBy(c => c.UtmCampaign)
            .Select(g => new
            {
                Name = g.Key ?? "unknown",
                BookingCount = g.Count(),
                Revenue = g.Sum(c => c.Revenue) ?? 0
            })
            .OrderByDescending(x => x.BookingCount)
            .Take(10) // Top 10 campaigns
            .ToListAsync();

        var totalCampaignBookings = utmCampaigns.Sum(x => x.BookingCount);

        var campaignStats = utmCampaigns.Select(x => new SourceStatisticDto
        {
            Name = x.Name,
            BookingCount = x.BookingCount,
            Revenue = x.Revenue,
            Percentage = totalCampaignBookings > 0 ? (double)x.BookingCount / totalCampaignBookings * 100 : 0
        }).ToList();

        // Get Top Referrers
        var topReferrers = await _context.PageViews
            .Where(p => p.ViewedAt >= startDate && p.ViewedAt <= endDate &&
                       p.ReferrerUrl != null && p.ReferrerUrl != "")
            .GroupBy(p => p.ReferrerUrl)
            .Select(g => new
            {
                Referrer = g.Key ?? "direct",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var referrerStats = topReferrers.Select(x => new ReferrerStatisticDto
        {
            Referrer = x.Referrer,
            Count = x.Count
        }).ToList();

        var result = new TrackingStatisticsDto
        {
            TotalBookings = totalBookings,
            BookingsWithTracking = bookingsWithTracking,
            UtmSources = sourceStats,
            UtmMediums = mediumStats,
            UtmCampaigns = campaignStats,
            TopReferrers = referrerStats,
            TotalRevenue = totalRevenue,
            AverageBookingValue = averageBookingValue
        };

        return Ok(result);
    }
}