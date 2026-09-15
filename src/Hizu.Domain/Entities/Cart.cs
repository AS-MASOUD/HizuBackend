using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
