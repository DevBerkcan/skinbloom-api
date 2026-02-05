namespace BarberDario.Api.Data.Entities;

public class ServiceBundle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal OriginalPrice { get; set; } // Sum of all included services
    public decimal BundlePrice { get; set; } // Discounted package price
    public decimal DiscountPercentage { get; set; } // Calculated: (OriginalPrice - BundlePrice) / OriginalPrice * 100
    public int TotalDurationMinutes { get; set; } // Sum of all service durations
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime? ValidFrom { get; set; } // Optional: Bundle valid from date
    public DateTime? ValidUntil { get; set; } // Optional: Bundle valid until date
    public string? TermsAndConditions { get; set; } // Optional terms
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ServiceBundleItem> BundleItems { get; set; } = new List<ServiceBundleItem>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
