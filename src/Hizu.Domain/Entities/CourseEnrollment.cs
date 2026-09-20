using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class CourseEnrollment : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; } // null for lifetime access
    public int ProgressPercentage { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
}
