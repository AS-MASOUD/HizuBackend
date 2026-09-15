using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
            throw new NotFoundException("Product", request.Id);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Sku = product.Sku,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Dimensions = product.Dimensions,
            Weight = product.Weight,
            Material = product.Material,
            Color = product.Color,
            Technique = product.Technique,
            Tolerance = product.Tolerance,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
        };
    }
}

public record GetProductBySlugQuery(string Slug) : IRequest<ProductDto>;

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLower();
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug, cancellationToken);

        if (product == null)
            throw new NotFoundException("Product", request.Slug);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Sku = product.Sku,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Dimensions = product.Dimensions,
            Weight = product.Weight,
            Material = product.Material,
            Color = product.Color,
            Technique = product.Technique,
            Tolerance = product.Tolerance,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategorySlug = product.Category.Slug,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
        };
    }
}

public record GetFeaturedProductsQuery(int Count = 4) : IRequest<List<ProductListDto>>;

public class GetFeaturedProductsQueryHandler : IRequestHandler<GetFeaturedProductsQuery, List<ProductListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturedProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductListDto>> Handle(GetFeaturedProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedAt)
            .Take(request.Count)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ShortDescription = p.ShortDescription,
                Sku = p.Sku,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                StockQuantity = p.StockQuantity,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                Material = p.Material,
                Technique = p.Technique,
                Tolerance = p.Tolerance,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CategorySlug = p.Category.Slug
            })
            .ToListAsync(cancellationToken);
    }
}
