using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Categories.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Categories.Queries.GetCategories;

public record GetCategoriesQuery(bool OnlyActive = true) : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCategoriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .AsQueryable();

        if (request.OnlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Code = c.Code,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(cancellationToken);

        // Build hierarchical tree
        var lookup = categories.ToLookup(c => c.ParentCategoryId);
        foreach (var cat in categories)
        {
            cat.SubCategories = lookup[cat.Id].ToList();
        }

        // Return root categories (or all if flat view is needed)
        return categories;
    }
}
