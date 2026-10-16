using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Orders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId, Guid? UserId = null) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Course)
            .Where(o => o.Id == request.OrderId);

        if (request.UserId.HasValue)
        {
            query = query.Where(o => o.UserId == request.UserId.Value);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);

        if (order == null)
            throw new NotFoundException("Order", request.OrderId);

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.User != null ? $"{order.User.FirstName} {order.User.LastName}".Trim() : string.Empty,
            CustomerEmail = order.User?.Email ?? string.Empty,
            CustomerPhone = order.User?.PhoneNumber ?? string.Empty,
            Status = order.Status,
            IsStockDeducted = order.IsStockDeducted,
            Subtotal = order.Subtotal,
            ShippingCost = order.ShippingCost,
            Total = order.Total,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                CourseId = i.CourseId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                ImageUrl = i.Product != null ? i.Product.ImageUrl : i.Course?.CoverImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}

public record GetUserOrdersQuery(Guid UserId) : IRequest<List<OrderSummaryDto>>;

public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, List<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderSummaryDto>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == request.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                Total = o.Total,
                ItemCount = o.Items.Sum(i => i.Quantity),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetAllOrdersQuery : PagedQuery, IRequest<PagedResult<OrderDto>>
{
    public string? Search { get; init; }
}

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(o => o.OrderNumber.ToLower().Contains(search) ||
                                     (o.User != null && (o.User.Email!.ToLower().Contains(search) ||
                                                         o.User.FirstName.ToLower().Contains(search) ||
                                                         o.User.LastName.ToLower().Contains(search) ||
                                                         (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(search)))) ||
                                     o.Items.Any(i => i.ProductName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                UserId = o.UserId,
                CustomerName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim() : string.Empty,
                CustomerEmail = o.User != null ? o.User.Email ?? "" : "",
                CustomerPhone = o.User != null ? o.User.PhoneNumber ?? "" : "",
                Status = o.Status,
                IsStockDeducted = o.IsStockDeducted,
                Subtotal = o.Subtotal,
                ShippingCost = o.ShippingCost,
                Total = o.Total,
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    CourseId = i.CourseId,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    ImageUrl = i.Product != null ? i.Product.ImageUrl : i.Course != null ? i.Course.CoverImageUrl : null,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return PagedResult<OrderDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
