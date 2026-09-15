using Hizu.Domain.Enums;

namespace Hizu.Application.Features.SpecialOrders.DTOs;

public class SpecialOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid? UserId { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MaterialPreference { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public decimal? QuotedPrice { get; set; }

    public SpecialOrderStatus Status { get; set; }
    public string StatusText => Status switch
    {
        SpecialOrderStatus.Pending => "در انتظار بررسی اولیه کارگاه",
        SpecialOrderStatus.Reviewing => "در حال بررسی فنی و DFM",
        SpecialOrderStatus.Approved => "تایید شده و آماده شروع",
        SpecialOrderStatus.Rejected => "رد شده",
        SpecialOrderStatus.InProgress => "در حال ساخت و ماشین‌کاری",
        SpecialOrderStatus.Completed => "ساخت تکمیل و تحویل داده شد",
        _ => Status.ToString()
    };

    public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<SpecialOrderImageDto> Images { get; set; } = [];
}

public class SpecialOrderImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
}
