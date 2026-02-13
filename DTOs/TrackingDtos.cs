// BarberDario.Api/DTOs/TrackingDto.cs
namespace BarberDario.Api.DTOs;

public class TrackLinkClickDto
{
    public string LinkName { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public string? SessionId { get; set; }
}

public class SimplifiedTrackingStatisticsDto
{
    // Overview stats
    public int TotalBookings { get; set; }
    public int TotalPageViews { get; set; }
    public int TotalLinkClicks { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }

    // Link click statistics
    public List<LinkClickStatisticDto> LinkClicks { get; set; } = new();
}

public class LinkClickStatisticDto
{
    public string LinkName { get; set; } = string.Empty;
    public int ClickCount { get; set; }
    public double Percentage { get; set; }
}

public class RevenueStatisticsDto
{
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TodayBookings { get; set; }
    public int WeekBookings { get; set; }
    public int MonthBookings { get; set; }
}

public class TrackPageViewDto
{
    public string? PageUrl { get; set; }
    public string? ReferrerUrl { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public string? SessionId { get; set; }
}