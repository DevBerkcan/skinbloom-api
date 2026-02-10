namespace Skinbloom.Api.Data.Entities
{
    public class AnalyticsSummary
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateOnly SummaryDate { get; set; }
        public int TotalVisits { get; set; }
        public int UniqueVisitors { get; set; }
        public int PageViews { get; set; }
        public int Bookings { get; set; }
        public int ContactForms { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageSessionDuration { get; set; }
        public double BounceRate { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}
