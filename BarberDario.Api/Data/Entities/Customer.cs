namespace BarberDario.Api.Data.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastVisit { get; set; }
    public int TotalBookings { get; set; } = 0;
    public int NoShowCount { get; set; } = 0;
    public string? Notes { get; set; }

    // Newsletter subscription
    public bool NewsletterSubscribed { get; set; } = false;
    public DateTime? NewsletterSubscribedAt { get; set; }
    public DateTime? NewsletterUnsubscribedAt { get; set; }
    public string? UnsubscribeToken { get; set; } // Unique token for unsubscribe link

    // Loyalty & VIP
    public Guid? LoyaltyTierId { get; set; }
    public decimal TotalSpent { get; set; } = 0; // Total amount spent (calculated)
    public int LoyaltyPoints { get; set; } = 0; // Future: points system
    public bool IsVip { get; set; } = false; // Manual VIP flag

    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public CustomerLoyaltyTier? LoyaltyTier { get; set; }

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
