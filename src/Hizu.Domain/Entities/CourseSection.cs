using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class CourseSection : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;

    public ICollection<CourseEpisode> Episodes { get; set; } = new List<CourseEpisode>();
}
