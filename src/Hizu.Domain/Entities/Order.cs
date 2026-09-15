using Hizu.Domain.Common;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;

namespace Hizu.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public Address ShippingAddress { get; set; } = new();

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
