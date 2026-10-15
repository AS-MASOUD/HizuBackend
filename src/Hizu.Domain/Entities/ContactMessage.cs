using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class ContactMessage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public string? AdminNotes { get; set; }
    public DateTime? RepliedAt { get; set; }
}
