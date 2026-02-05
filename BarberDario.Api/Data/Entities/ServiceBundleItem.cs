namespace BarberDario.Api.Data.Entities;

public class ServiceBundleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BundleId { get; set; }
    public Guid ServiceId { get; set; }
    public int Quantity { get; set; } = 1; // How many times this service is included (e.g., 2x Hyaluron treatments)
    public int DisplayOrder { get; set; } = 0;
    public string? Notes { get; set; } // Optional notes about this service in the bundle

    // Navigation properties
    public ServiceBundle Bundle { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
