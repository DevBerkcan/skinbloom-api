namespace BarberDario.Api.Data.Entities;

public class ServiceCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; } // Optional icon name or emoji
    public string? Color { get; set; } // Optional color for UI (e.g., "#000000")
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
