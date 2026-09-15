using Hizu.Domain.Common;
using Hizu.Domain.Enums;

namespace Hizu.Domain.Entities;

public class SpecialOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MaterialPreference { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public decimal? QuotedPrice { get; set; }

    public SpecialOrderStatus Status { get; set; } = SpecialOrderStatus.Pending;

    public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }

    public ICollection<SpecialOrderImage> Images { get; set; } = new List<SpecialOrderImage>();
}
