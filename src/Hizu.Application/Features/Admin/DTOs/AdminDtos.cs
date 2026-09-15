namespace Hizu.Application.Features.Admin.DTOs;

public class AdminDashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int TotalSpecialOrders { get; set; }
    public int PendingSpecialOrders { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public int OrdersCount { get; set; }
}
