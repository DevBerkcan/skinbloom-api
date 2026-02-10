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

    // Navigation properties
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    // Computed property
    public string FullName => $"{FirstName} {LastName}";
}
