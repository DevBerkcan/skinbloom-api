namespace BarberDario.Api.Data.Entities;

public class NewsletterRecipient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NewsletterId { get; set; }
    public Guid CustomerId { get; set; }
    public string Email { get; set; } = string.Empty; // Store email at time of sending
    public bool Sent { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public bool Opened { get; set; } = false; // Tracking pixel
    public DateTime? OpenedAt { get; set; }
    public bool Clicked { get; set; } = false; // Clicked any link
    public DateTime? ClickedAt { get; set; }
    public bool Failed { get; set; } = false;
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public Newsletter Newsletter { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
