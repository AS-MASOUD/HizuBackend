using Hizu.Domain.Common;
using Hizu.Domain.Enums;

namespace Hizu.Domain.Entities;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Description { get; set; } = string.Empty;

    // Pricing & Reservation
    public decimal? Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public bool IsReservationOnly { get; set; } = false;

    // Media & File Server Integration
    public string? CoverImageUrl { get; set; }
    public string? IntroVideoUrl { get; set; } // Sample / trailer video stream or download URL
    public string? FileServerBaseUrl { get; set; } // Base origin or CDN endpoint e.g. https://files.hizu.ir
    public string? StoragePath { get; set; } // File server folder path e.g. /courses/cad-fusion360/
    public string? DownloadableFilesUrl { get; set; } // Resource package / project files archive

    // Course Metadata
    public int DurationMinutes { get; set; }
    public string? DurationText { get; set; } // e.g. "۱۶ ساعت و ۴۵ دقیقه"
    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;
    public string Language { get; set; } = "فارسی";
    public int TotalEpisodesCount { get; set; }
    public int DownloadableResourcesCount { get; set; }
    public bool IncludesCertificate { get; set; } = true;
    public bool HasLifetimeAccess { get; set; } = true;

    // Instructor Information
    public string InstructorName { get; set; } = "تیم فنی مهندسی هیزو";
    public string? InstructorTitle { get; set; } = "استودیو طراحی و ساخت دیجیتال هیزو";
    public string? InstructorBio { get; set; }
    public string? InstructorAvatarUrl { get; set; }

    // Syllabus & Educational Outcomes
    public string? WhatYouWillLearn { get; set; } // JSON or newline-delimited list of outcomes
    public string? Requirements { get; set; } // Prerequisites
    public string? TargetAudience { get; set; }

    // Publication & Metrics
    public bool IsPublished { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public decimal AverageRating { get; set; } = 5.0m;
    public int RatingCount { get; set; } = 0;
    public int StudentsCount { get; set; } = 0;

    // Navigation Collections
    public ICollection<CourseSection> Sections { get; set; } = new List<CourseSection>();
    public ICollection<CourseEpisode> Episodes { get; set; } = new List<CourseEpisode>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
