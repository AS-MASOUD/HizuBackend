using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;

namespace Hizu.Application.Features.Orders.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string StatusText => Status switch
    {
        OrderStatus.Pending => "در انتظار پرداخت / تایید",
        OrderStatus.Processing => "در حال آماده‌سازی و ساخت",
        OrderStatus.Shipped => "ارسال شده",
        OrderStatus.Delivered => "تحویل شده",
        OrderStatus.Cancelled => "لغو شده",
        _ => Status.ToString()
    };

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string StatusText => Status switch
    {
        OrderStatus.Pending => "در انتظار پرداخت / تایید",
        OrderStatus.Processing => "در حال آماده‌سازی و ساخت",
        OrderStatus.Shipped => "ارسال شده",
        OrderStatus.Delivered => "تحویل شده",
        OrderStatus.Cancelled => "لغو شده",
        _ => Status.ToString()
    };
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
