using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Cart.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Cart.Queries;

public record GetCartQuery(Guid UserId) : IRequest<CartDto>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto>
{
    private readonly IApplicationDbContext _context;

    public GetCartQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.Course)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
        {
            return new CartDto
            {
                Id = Guid.Empty,
                UserId = request.UserId,
                Items = []
            };
        }

        var validItems = new List<CartItemDto>();

        foreach (var item in cart.Items)
        {
            if (item.Product != null && item.Product.IsActive)
            {
                validItems.Add(new CartItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    CourseId = null,
                    ItemType = "Product",
                    ProductName = item.Product.Name,
                    ProductSlug = item.Product.Slug,
                    Sku = item.Product.Sku,
                    ImageUrl = item.Product.ImageUrl,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity,
                    AvailableStock = item.Product.StockQuantity
                });
            }
            else if (item.Course != null && item.Course.IsPublished)
            {
                validItems.Add(new CartItemDto
                {
                    Id = item.Id,
                    ProductId = null,
                    CourseId = item.CourseId,
                    ItemType = "Course",
                    ProductName = item.Course.Title,
                    ProductSlug = item.Course.Slug,
                    Sku = $"CRS-{item.Course.Slug.ToUpper()}",
                    ImageUrl = item.Course.CoverImageUrl,
                    UnitPrice = item.Course.Price ?? 0m,
                    Quantity = 1,
                    AvailableStock = 9999
                });
            }
        }

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = validItems
        };
    }
}
