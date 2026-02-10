namespace BarberDario.Api.Data.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }

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

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public Service Service { get; set; } = null!;
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
