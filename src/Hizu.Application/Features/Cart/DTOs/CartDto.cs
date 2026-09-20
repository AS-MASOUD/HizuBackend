namespace Hizu.Application.Features.Cart.DTOs;

public class CartDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.TotalPrice);
    // Digital courses do not require physical shipping. Free shipping if all items are courses or subtotal > 1,500,000.
    public decimal ShippingCost => (Items.Count == 0 || Items.All(i => i.CourseId != null) || Subtotal > 1500000) ? 0 : 75000;
    public decimal Total => Subtotal + ShippingCost;
    public int TotalItemCount => Items.Sum(i => i.Quantity);
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? CourseId { get; set; }
    public string ItemType { get; set; } = "Product"; // "Product" or "Course"
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public int AvailableStock { get; set; } = 9999;
}
