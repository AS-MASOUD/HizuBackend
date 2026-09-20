using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class ProductComment : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } = 5; // 1 to 5 stars

    public bool IsApproved { get; set; } = false;

    // Official Admin Response only (no user-to-user chat)
    public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }
    public Guid? AdminUserId { get; set; }
}
