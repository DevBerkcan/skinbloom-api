// DTOs
public class TrackPageViewDto
{
    public string PageUrl { get; set; } = string.Empty;
    public string? ReferrerUrl { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public string? SessionId { get; set; }
}

public class TrackConversionDto
{
    public Guid BookingId { get; set; }
    public string ConversionType { get; set; } = "booking";
    public string? PageUrl { get; set; }
    public string? ReferrerUrl { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public decimal? Revenue { get; set; }
}

public class TrackingStatisticsDto
{
    public int TotalBookings { get; set; }
    public int BookingsWithTracking { get; set; }
    public List<SourceStatisticDto> UtmSources { get; set; } = new();
    public List<SourceStatisticDto> UtmMediums { get; set; } = new();
    public List<SourceStatisticDto> UtmCampaigns { get; set; } = new();
    public List<ReferrerStatisticDto> TopReferrers { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal AverageBookingValue { get; set; }
}

public class SourceStatisticDto
{
    public string Name { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
    public double Percentage { get; set; }
}

public class ReferrerStatisticDto
{
    public string Referrer { get; set; } = string.Empty;
    public int Count { get; set; }
}