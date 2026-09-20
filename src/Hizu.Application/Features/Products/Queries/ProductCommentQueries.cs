using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Products.Queries;

public record GetProductCommentsQuery(Guid ProductId) : IRequest<ProductCommentSummaryDto>;

public class GetProductCommentsQueryHandler : IRequestHandler<GetProductCommentsQuery, ProductCommentSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductCommentSummaryDto> Handle(GetProductCommentsQuery request, CancellationToken cancellationToken)
    {
        var comments = await _context.ProductComments
            .AsNoTracking()
            .Where(c => c.ProductId == request.ProductId && c.IsApproved)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ProductCommentDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                AuthorName = c.AuthorName,
                Content = c.Content,
                Rating = c.Rating,
                IsApproved = c.IsApproved,
                AdminReply = c.AdminReply,
                AdminRepliedAt = c.AdminRepliedAt,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        double avgRating = comments.Count > 0
            ? Math.Round(comments.Average(c => c.Rating), 1)
            : 5.0;

        return new ProductCommentSummaryDto
        {
            AverageRating = avgRating,
            TotalComments = comments.Count,
            Comments = comments
        };
    }
}

public record GetAdminCommentsQuery(bool? IsApproved = null, int Page = 1, int PageSize = 50) : IRequest<PagedResult<ProductCommentDto>>;

public class GetAdminCommentsQueryHandler : IRequestHandler<GetAdminCommentsQuery, PagedResult<ProductCommentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ProductCommentDto>> Handle(GetAdminCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProductComments
            .Include(c => c.Product)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsApproved.HasValue)
        {
            query = query.Where(c => c.IsApproved == request.IsApproved.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ProductCommentDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : "نامشخص",
                AuthorName = c.AuthorName,
                AuthorEmail = c.AuthorEmail,
                Content = c.Content,
                Rating = c.Rating,
                IsApproved = c.IsApproved,
                AdminReply = c.AdminReply,
                AdminRepliedAt = c.AdminRepliedAt,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductCommentDto>(items, totalCount, request.Page, request.PageSize);
    }
}
