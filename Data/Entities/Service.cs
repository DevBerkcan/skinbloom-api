using Skinbloom.Api.Data.Entities;

namespace BarberDario.Api.Data.Entities;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ServiceCategory Category { get; set; } = null!;

    // Many-to-many relationship with employees
    public ICollection<ServiceEmployee> ServiceEmployees { get; set; } = new List<ServiceEmployee>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}