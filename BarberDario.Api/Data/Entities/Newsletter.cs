namespace BarberDario.Api.Data.Entities;

public class Newsletter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PreviewText { get; set; } // Preview text shown in email clients
    public NewsletterStatus Status { get; set; } = NewsletterStatus.Draft;
    public DateTime? ScheduledFor { get; set; } // When to send (null = send immediately)
    public DateTime? SentAt { get; set; }
    public int RecipientCount { get; set; } = 0; // How many customers received it
    public int OpenedCount { get; set; } = 0; // How many opened (requires tracking)
    public int ClickedCount { get; set; } = 0; // How many clicked links (requires tracking)
    public Guid CreatedBy { get; set; } // Admin user who created it
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<NewsletterRecipient> Recipients { get; set; } = new List<NewsletterRecipient>();
}

public enum NewsletterStatus
{
    Draft,      // Being edited
    Scheduled,  // Scheduled to send later
    Sending,    // Currently being sent
    Sent,       // Successfully sent
    Failed      // Failed to send
}
