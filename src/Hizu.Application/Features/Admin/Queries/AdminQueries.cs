using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Admin.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Admin.Queries;

public record GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsDto>;

public class GetAdminDashboardStatsQueryHandler : IRequestHandler<GetAdminDashboardStatsQuery, AdminDashboardStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminDashboardStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardStatsDto> Handle(GetAdminDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var totalCategories = await _context.Categories.CountAsync(cancellationToken);
        var totalOrders = await _context.Orders.CountAsync(cancellationToken);
        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing, cancellationToken);
        var totalSpecialOrders = await _context.SpecialOrders.CountAsync(cancellationToken);
        var pendingSpecialOrders = await _context.SpecialOrders.CountAsync(s => s.Status == SpecialOrderStatus.Pending || s.Status == SpecialOrderStatus.Reviewing, cancellationToken);
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalRevenue = await _context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.Total, cancellationToken) ?? 0;

        return new AdminDashboardStatsDto
        {
            TotalProducts = totalProducts,
            TotalCategories = totalCategories,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            TotalSpecialOrders = totalSpecialOrders,
            PendingSpecialOrders = pendingSpecialOrders,
            TotalUsers = totalUsers,
            TotalRevenue = totalRevenue
        };
    }
}

public class GetAdminUsersQuery : PagedQuery, IRequest<PagedResult<AdminUserDto>>
{
    public string? Search { get; init; }
}

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PagedResult<AdminUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetAdminUsersQueryHandler(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResult<AdminUserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.Orders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => (u.Email != null && u.Email.ToLower().Contains(search)) ||
                                     u.FirstName.ToLower().Contains(search) ||
                                     u.LastName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var resultItems = new List<AdminUserDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            resultItems.Add(new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = $"{u.FirstName} {u.LastName}".Trim(),
                PhoneNumber = u.PhoneNumber,
                Roles = roles.ToList(),
                CreatedAt = u.CreatedAt,
                OrdersCount = u.Orders.Count
            });
        }

        return PagedResult<AdminUserDto>.Create(resultItems, totalCount, request.Page, request.PageSize);
    }
}
