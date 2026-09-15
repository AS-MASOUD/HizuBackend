using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.SpecialOrders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.SpecialOrders.Queries;

public record GetSpecialOrderByIdQuery(Guid Id, Guid? UserId = null) : IRequest<SpecialOrderDto>;

public class GetSpecialOrderByIdQueryHandler : IRequestHandler<GetSpecialOrderByIdQuery, SpecialOrderDto>
{
    private readonly IApplicationDbContext _context;

    public GetSpecialOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SpecialOrderDto> Handle(GetSpecialOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SpecialOrders
            .AsNoTracking()
            .Include(s => s.Images)
            .Where(s => s.Id == request.Id);

        if (request.UserId.HasValue)
        {
            query = query.Where(s => s.UserId == request.UserId.Value);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);
        if (order == null)
            throw new NotFoundException("SpecialOrder", request.Id);

        return new SpecialOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            CustomerEmail = order.CustomerEmail,
            Title = order.Title,
            Category = order.Category,
            Description = order.Description,
            MaterialPreference = order.MaterialPreference,
            EstimatedBudget = order.EstimatedBudget,
            QuotedPrice = order.QuotedPrice,
            Status = order.Status,
            AdminReply = order.AdminReply,
            AdminRepliedAt = order.AdminRepliedAt,
            CreatedAt = order.CreatedAt,
            Images = order.Images.Select(i => new SpecialOrderImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                OriginalFileName = i.OriginalFileName,
                FileSizeBytes = i.FileSizeBytes
            }).ToList()
        };
    }
}

public record GetUserSpecialOrdersQuery(Guid UserId) : IRequest<List<SpecialOrderDto>>;

public class GetUserSpecialOrdersQueryHandler : IRequestHandler<GetUserSpecialOrdersQuery, List<SpecialOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserSpecialOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpecialOrderDto>> Handle(GetUserSpecialOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.SpecialOrders
            .AsNoTracking()
            .Include(s => s.Images)
            .Where(s => s.UserId == request.UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(order => new SpecialOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                Title = order.Title,
                Category = order.Category,
                Description = order.Description,
                MaterialPreference = order.MaterialPreference,
                EstimatedBudget = order.EstimatedBudget,
                QuotedPrice = order.QuotedPrice,
                Status = order.Status,
                AdminReply = order.AdminReply,
                AdminRepliedAt = order.AdminRepliedAt,
                CreatedAt = order.CreatedAt,
                Images = order.Images.Select(i => new SpecialOrderImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    OriginalFileName = i.OriginalFileName,
                    FileSizeBytes = i.FileSizeBytes
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetAllSpecialOrdersQuery : PagedQuery, IRequest<PagedResult<SpecialOrderDto>>
{
    public string? Search { get; init; }
    public string? Category { get; init; }
}

public class GetAllSpecialOrdersQueryHandler : IRequestHandler<GetAllSpecialOrdersQuery, PagedResult<SpecialOrderDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllSpecialOrdersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SpecialOrderDto>> Handle(GetAllSpecialOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SpecialOrders
            .AsNoTracking()
            .Include(s => s.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(s => s.Category == request.Category);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.OrderNumber.ToLower().Contains(search) ||
                                     s.CustomerName.ToLower().Contains(search) ||
                                     s.CustomerPhone.Contains(search) ||
                                     s.Title.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(order => new SpecialOrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                Title = order.Title,
                Category = order.Category,
                Description = order.Description,
                MaterialPreference = order.MaterialPreference,
                EstimatedBudget = order.EstimatedBudget,
                QuotedPrice = order.QuotedPrice,
                Status = order.Status,
                AdminReply = order.AdminReply,
                AdminRepliedAt = order.AdminRepliedAt,
                CreatedAt = order.CreatedAt,
                Images = order.Images.Select(i => new SpecialOrderImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    OriginalFileName = i.OriginalFileName,
                    FileSizeBytes = i.FileSizeBytes
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return PagedResult<SpecialOrderDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
