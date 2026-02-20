// Data/Entities/Employee.cs
namespace BarberDario.Api.Data.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? Username { get; set; } 
    public string? PasswordHash { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<BlockedTimeSlot> BlockedTimeSlots { get; set; } = new List<BlockedTimeSlot>();
}