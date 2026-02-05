namespace BarberDario.Api.Data.Entities;

/// <summary>
/// Loyalty tiers for customers based on booking count or total spent
/// </summary>
public class CustomerLoyaltyTier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // z.B. "Bronze", "Silber", "Gold"
    public string? Description { get; set; }
    public int MinBookings { get; set; } = 0; // Minimum bookings to reach this tier
    public decimal DiscountPercentage { get; set; } = 0; // Discount in percentage
    public string? Color { get; set; } // UI color
    public string? Icon { get; set; } // UI icon
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Benefits
    public bool PriorityBooking { get; set; } = false; // Can book earlier than others
    public bool SpecialOffers { get; set; } = false; // Receives exclusive offers
    public int BonusPointsMultiplier { get; set; } = 1; // Future: points system

    // Navigation
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
