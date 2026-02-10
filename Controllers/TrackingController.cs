// BarberDario.Api.Controllers/TrackingController.cs
using BarberDario.Api.Data;
using BarberDario.Api.DTOs;
using BarberDario.Api.Services;
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

    // Ersetze die LINQ-Selects mit "await" durch explizite asynchrone Schleifen, da "await" nicht direkt in LINQ-Ausdrücken verwendet werden kann.

    // --- Ersetze im GetAnalytics-Endpoint: ---
    [HttpGet("analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalyticsDashboardDto>> GetAnalytics(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var totalVisits = await _context.PageViews
            .Where(p => p.ViewedAt >= start && p.ViewedAt <= end)
            .CountAsync();

        var uniqueVisitors = await _context.PageViews
            .Where(p => p.ViewedAt >= start && p.ViewedAt <= end)
            .Select(p => p.SessionId)
            .Distinct()
            .CountAsync();

        var totalBookings = await _context.Conversions
            .Where(c => c.ConvertedAt >= start && c.ConvertedAt <= end &&
                       c.ConversionType == "booking")
            .CountAsync();

        var totalRevenue = await _context.Conversions
            .Where(c => c.ConvertedAt >= start && c.ConvertedAt <= end &&
                       c.ConversionType == "booking" && c.Revenue.HasValue)
            .SumAsync(c => c.Revenue) ?? 0;

        var trafficSources = await _context.PageViews
            .Where(p => p.ViewedAt >= start && p.ViewedAt <= end && p.UtmSource != null)
            .GroupBy(p => p.UtmSource)
            .Select(g => new TrafficSourceDto(
                g.Key ?? "Direct",
                g.Count(),
                (int)Math.Round((double)g.Count() / totalVisits * 100)
            ))
            .ToListAsync();

        // Fix: Ersetze asynchrones Select durch explizite Schleife
        var conversionGroups = await _context.Conversions
            .Where(c => c.ConvertedAt >= start && c.ConvertedAt <= end &&
                       c.ConversionType == "booking" && c.UtmSource != null)
            .GroupBy(c => c.UtmSource)
            .ToListAsync();

        var conversionRates = new List<ConversionRateDto>();
        foreach (var g in conversionGroups)
        {
            var visits = await _context.PageViews
                .Where(p => p.ViewedAt >= start && p.ViewedAt <= end &&
                           p.UtmSource == g.Key)
                .CountAsync();

            conversionRates.Add(new ConversionRateDto(
                g.Key ?? "Direct",
                g.Count(),
                visits
            ));
        }

        var dashboard = new AnalyticsDashboardDto(
            totalVisits,
            uniqueVisitors,
            totalBookings,
            totalRevenue,
            trafficSources,
            conversionRates
        );

        return Ok(dashboard);
    }

    // --- Ersetze im GetCampaignPerformance-Endpoint: ---
    [HttpGet("campaigns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CampaignPerformanceDto>>> GetCampaignPerformance(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var campaignGroups = await _context.Conversions
            .Where(c => c.ConvertedAt >= start && c.ConvertedAt <= end &&
                       c.ConversionType == "booking" &&
                       c.UtmCampaign != null)
            .GroupBy(c => new { c.UtmCampaign, c.UtmSource, c.UtmMedium })
            .ToListAsync();

        var campaignPerformance = new List<CampaignPerformanceDto>();
        foreach (var g in campaignGroups)
        {
            var visits = await _context.PageViews
                .Where(p => p.ViewedAt >= start && p.ViewedAt <= end &&
                           p.UtmCampaign == g.Key.UtmCampaign)
                .CountAsync();

            campaignPerformance.Add(new CampaignPerformanceDto(
                g.Key.UtmCampaign ?? "Unknown",
                g.Key.UtmSource ?? "Unknown",
                g.Key.UtmMedium ?? "Unknown",
                g.Count(),
                g.Sum(c => c.Revenue) ?? 0,
                visits
            ));
        }

        return Ok(campaignPerformance);
    }
}