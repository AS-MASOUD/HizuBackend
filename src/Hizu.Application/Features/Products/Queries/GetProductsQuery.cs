using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Products.Queries;

public class GetProductsQuery : PagedQuery, IRequest<PagedResult<ProductListDto>>
{
    public int? CategoryId { get; init; }
    public string? CategorySlug { get; init; }
    public string? Search { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Material { get; init; }
    public string? Technique { get; init; }
    public bool? IsFeatured { get; init; }
    public bool OnlyActive { get; init; } = true;
    public string? Sort { get; init; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductListDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (request.OnlyActive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == request.CategoryId.Value || 
                                     p.Category.ParentCategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var catSlug = request.CategorySlug.Trim().ToLower();
            query = query.Where(p => p.Category.Slug == catSlug || 
                                     (p.Category.ParentCategory != null && p.Category.ParentCategory.Slug == catSlug));
        }

        if (request.IsFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == request.IsFeatured.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Material))
        {
            var mat = request.Material.Trim().ToLower();
            query = query.Where(p => p.Material != null && p.Material.ToLower().Contains(mat));
        }

        if (!string.IsNullOrWhiteSpace(request.Technique))
        {
            var tech = request.Technique.Trim().ToLower();
            query = query.Where(p => p.Technique != null && p.Technique.ToLower().Contains(tech));
        }

        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) ||
                                     p.Description.ToLower().Contains(search) ||
                                     p.Sku.ToLower().Contains(search) ||
                                     p.Category.Name.ToLower().Contains(search) ||
                                     (p.Material != null && p.Material.ToLower().Contains(search)) ||
                                     (p.Technique != null && p.Technique.ToLower().Contains(search)));
        }

        query = request.Sort?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "featured" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return PagedResult<ProductListDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
