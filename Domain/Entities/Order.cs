namespace Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Changed to string to match IdentityUser
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Delivered, Cancelled
    public string ShippingAddress { get; set; } = string.Empty;
    public string? ShippingCity { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; } = "North Macedonia";

    // Navigation properties
    // User navigation removed - using IdentityUser instead
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
