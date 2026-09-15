using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Orders.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Orders.Commands;

public record CreateOrderCommand : IRequest<OrderDto>
{
    public Guid UserId { get; init; }
    public Address ShippingAddress { get; init; } = new();
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ShippingAddress.Street).NotEmpty().WithMessage("آدرس پستی الزامی است.");
        RuleFor(x => x.ShippingAddress.City).NotEmpty().WithMessage("نام شهر الزامی است.");
        RuleFor(x => x.ShippingAddress.State).NotEmpty().WithMessage("نام استان الزامی است.");
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null || !cart.Items.Any())
            throw new BadRequestException("سبد خرید شما خالی است.");

        var user = await _context.Users.FindAsync([request.UserId], cancellationToken);
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        // Transactional execution with atomic inventory deduction
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync([item.ProductId], cancellationToken);
                if (product == null || !product.IsActive)
                    throw new BadRequestException($"محصول '{item.Product?.Name ?? "نامشخص"}' در حال حاضر موجود یا فعال نیست.");

                if (product.StockQuantity < item.Quantity)
                    throw new BadRequestException($"موجودی محصول '{product.Name}' برای تعداد درخواستی کافی نیست. موجودی انبار: {product.StockQuantity}");

                // Atomic stock reduction
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                var itemTotal = product.Price * item.Quantity;
                subtotal += itemTotal;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price, // Preserves price at purchase time
                    TotalPrice = itemTotal,
                    CreatedAt = DateTime.UtcNow
                });
            }

            decimal shippingCost = subtotal > 1500000 ? 0 : 75000;
            decimal total = subtotal + shippingCost;

            var orderNumber = $"HIZU-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var order = new Order
            {
                UserId = request.UserId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Processing, // Direct to processing upon creation
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Total = total,
                ShippingAddress = request.ShippingAddress,
                Items = orderItems,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            // Clear the user's cart
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = $"{user.FirstName} {user.LastName}".Trim(),
                CustomerEmail = user.Email ?? string.Empty,
                Status = order.Status,
                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                ShippingAddress = order.ShippingAddress,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<bool>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FindAsync([request.OrderId], cancellationToken);
        if (order == null)
            throw new NotFoundException("Order", request.OrderId);

        order.Status = request.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
