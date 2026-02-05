namespace BarberDario.Api.Data.Entities;

public class Waitlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? ServiceId { get; set; } // Nullable - customer might want any available slot
    public Guid? BundleId { get; set; }
    public DateOnly? PreferredDate { get; set; } // Nullable - customer might be flexible
    public TimeOnly? PreferredTimeFrom { get; set; } // Preferred time range start
    public TimeOnly? PreferredTimeTo { get; set; } // Preferred time range end
    public string? Notes { get; set; }
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NotifiedAt { get; set; } // When customer was notified about availability
    public DateTime? ConvertedAt { get; set; } // When converted to actual booking
    public Guid? ConvertedToBookingId { get; set; } // Which booking it became
    public DateTime? ExpiredAt { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Service? Service { get; set; }
    public ServiceBundle? Bundle { get; set; }
    public Booking? ConvertedToBooking { get; set; }
}

public enum WaitlistStatus
{
    Active,      // Waiting for slot
    Notified,    // Customer was notified about availability
    Converted,   // Converted to actual booking
    Expired,     // Customer didn't respond in time
    Cancelled    // Customer cancelled waitlist entry
}
