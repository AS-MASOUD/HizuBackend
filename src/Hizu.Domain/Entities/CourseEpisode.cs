using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class CourseEpisode : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public Guid? SectionId { get; set; }
    public CourseSection? Section { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int DurationMinutes { get; set; }
    public string? DurationText { get; set; } // e.g. "۱۸:۳۵"

    // File Server & Stream URLs
    public string? VideoUrl { get; set; } // Path or full URL on file server
    public string? FileServerUrl { get; set; } // Direct origin or mirror URL
    public string? AttachmentUrl { get; set; } // Additional resources (CAD, STL, G-code, PDF)
    public string? AttachmentName { get; set; }
    public long? AttachmentSizeBytes { get; set; }

    public bool IsFreePreview { get; set; } = false; // Sample episode available before purchase
    public int DisplayOrder { get; set; } = 0;
}
