using Hizu.Domain.Enums;

namespace Hizu.Application.Features.Courses.DTOs;

public class CourseListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? IntroVideoUrl { get; set; }
    public int DurationMinutes { get; set; }
    public string? DurationText { get; set; }
    public CourseLevel Level { get; set; }
    public string LevelTitle => Level switch
    {
        CourseLevel.Beginner => "مقدماتی",
        CourseLevel.Intermediate => "متوسط",
        CourseLevel.Advanced => "پیشرفته",
        CourseLevel.AllLevels => "از مقدماتی تا پیشرفته",
        _ => "عمومی"
    };
    public string InstructorName { get; set; } = string.Empty;
    public int TotalEpisodesCount { get; set; }
    public decimal AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int StudentsCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class CourseDto : CourseListDto
{
    public string Description { get; set; } = string.Empty;
    public string? FileServerBaseUrl { get; set; }
    public string? StoragePath { get; set; }
    public string? DownloadableFilesUrl { get; set; }
    public string Language { get; set; } = "فارسی";
    public int DownloadableResourcesCount { get; set; }
    public bool IncludesCertificate { get; set; }
    public bool HasLifetimeAccess { get; set; }
    public string? InstructorTitle { get; set; }
    public string? InstructorBio { get; set; }
    public string? InstructorAvatarUrl { get; set; }

    public List<string> WhatYouWillLearnList { get; set; } = [];
    public List<string> RequirementsList { get; set; } = [];
    public List<string> TargetAudienceList { get; set; } = [];

    public List<CourseSectionDto> Sections { get; set; } = [];
    public List<CourseEpisodeDto> SampleEpisodes { get; set; } = [];
}

public class CourseSectionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<CourseEpisodeDto> Episodes { get; set; } = [];
}

public class CourseEpisodeDto
{
    public Guid Id { get; set; }
    public Guid? SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string? DurationText { get; set; }
    public string? VideoUrl { get; set; }
    public string? FileServerUrl { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public long? AttachmentSizeBytes { get; set; }
    public bool IsFreePreview { get; set; }
    public int DisplayOrder { get; set; }
}
