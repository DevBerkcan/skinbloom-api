// BarberDario.Api.DTOs/TrackingDtos.cs
namespace BarberDario.Api.DTOs;

public record TrackPageViewDto(
    string PageUrl,
    string? ReferrerUrl = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmContent = null,
    string? UtmTerm = null,
    string? SessionId = null
);

public record TrackConversionDto(
    string ConversionType,
    Guid? BookingId = null,
    string? PageUrl = null,
    string? ReferrerUrl = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmContent = null,
    string? UtmTerm = null,
    decimal? Revenue = null
);

public record AnalyticsDashboardDto(
    int TotalVisits,
    int UniqueVisitors,
    int TotalBookings,
    decimal TotalRevenue,
    List<TrafficSourceDto> TrafficSources,
    List<ConversionRateDto> ConversionRates
);

public record TrafficSourceDto(
    string Source,
    int Visits,
    int Percentage
);

public record ConversionRateDto(
    string Source,
    int Conversions,
    int Visits
)
{
    public double Rate => Visits > 0 ? Math.Round((double)Conversions / Visits * 100, 2) : 0;
}

public record CampaignPerformanceDto(
    string Campaign,
    string Source,
    string Medium,
    int Conversions,
    decimal Revenue,
    int Visits
)
{
    public double ConversionRate => Visits > 0 ? Math.Round((double)Conversions / Visits * 100, 2) : 0;
    public decimal RevenuePerVisit => Visits > 0 ? Revenue / Visits : 0;
}