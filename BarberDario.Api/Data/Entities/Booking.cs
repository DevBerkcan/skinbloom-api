namespace BarberDario.Api.Data.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? ServiceId { get; set; } // Nullable: either Service or Bundle
    public Guid? BundleId { get; set; } // Nullable: either Service or Bundle

    // Termin-Details
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // Status
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    // Kommunikation
    public DateTime? ConfirmationSentAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }

    // Notizen
    public string? CustomerNotes { get; set; }
    public string? AdminNotes { get; set; }

    // Tracking (woher kam die Buchung?)
    public string? ReferrerUrl { get; set; }
    public string? UtmSource { get; set; }      // z.B. "instagram", "google", "facebook"
    public string? UtmMedium { get; set; }      // z.B. "social", "cpc", "email"
    public string? UtmCampaign { get; set; }    // z.B. "winter_promo", "grand_opening"
    public string? UtmContent { get; set; }     // z.B. "banner_red", "story_video"
    public string? UtmTerm { get; set; }        // z.B. "barber_berlin", "herrenfriseur"

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Service? Service { get; set; }
    public ServiceBundle? Bundle { get; set; }
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

    // Computed property
    public string BookingNumber => $"BK-{BookingDate:yyyyMMdd}-{Id.ToString()[..8].ToUpper()}";
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed,
    NoShow
}
