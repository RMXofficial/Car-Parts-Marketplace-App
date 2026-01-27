namespace Domain.Entities;

public class Listing
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD"; // Default to USD
    public int CategoryId { get; set; }
    public string ListingType { get; set; } = string.Empty; // Car, Motorcycle, Part
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Condition { get; set; } // New, Used, For Parts
    public string? ImageUrl { get; set; }
    public string UserId { get; set; } = string.Empty; // Changed to string to match IdentityUser
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    // User navigation removed - using IdentityUser instead
    public Category Category { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
