using Hizu.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Hizu.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<SpecialOrder> SpecialOrders { get; set; } = new List<SpecialOrder>();
}
