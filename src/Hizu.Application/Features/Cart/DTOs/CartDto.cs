namespace Hizu.Application.Features.Cart.DTOs;

public class CartDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<CartItemDto> Items { get; set; } = [];
    public decimal Subtotal => Items.Sum(i => i.TotalPrice);
    public decimal ShippingCost => Subtotal > 1500000 || Items.Count == 0 ? 0 : 75000;
    public decimal Total => Subtotal + ShippingCost;
    public int TotalItemCount => Items.Sum(i => i.Quantity);
}

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public int AvailableStock { get; set; }
}
