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

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items
                .Where(i => i.Product != null && i.Product.IsActive)
                .Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSlug = i.Product.Slug,
                    Sku = i.Product.Sku,
                    ImageUrl = i.Product.ImageUrl,
                    UnitPrice = i.Product.Price, // Authoritative current price from Product record
                    Quantity = i.Quantity,
                    AvailableStock = i.Product.StockQuantity
                }).ToList()
        };
    }
}
