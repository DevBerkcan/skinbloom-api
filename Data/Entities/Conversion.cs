using BarberDario.Api.Data.Entities;

namespace Skinbloom.Api.Data.Entities
{
    public class Conversion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? BookingId { get; set; }
        public string ConversionType { get; set; } = string.Empty; // "booking", "contact", "newsletter"
        public string? PageUrl { get; set; }
        public string? ReferrerUrl { get; set; }
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public string? UtmContent { get; set; }
        public string? UtmTerm { get; set; }
        public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;
        public decimal? Revenue { get; set; }

        // Navigation property
        public Booking? Booking { get; set; }
    }
}
