using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Courses.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Courses.Queries;

public record GetCoursesQuery(
    string? Search = null,
    CourseLevel? Level = null,
    int Page = 1,
    int PageSize = 12) : IRequest<PagedResult<CourseListDto>>;

public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<CourseListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCoursesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CourseListDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Title.ToLower().Contains(search) ||
                (c.Subtitle != null && c.Subtitle.ToLower().Contains(search)) ||
                c.Description.ToLower().Contains(search) ||
                c.InstructorName.ToLower().Contains(search));
        }

        if (request.Level.HasValue)
        {
            query = query.Where(c => c.Level == request.Level.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CourseListDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Subtitle = c.Subtitle,
                Price = c.Price,
                CompareAtPrice = c.CompareAtPrice,
                CoverImageUrl = c.CoverImageUrl,
                IntroVideoUrl = c.IntroVideoUrl,
                DurationMinutes = c.DurationMinutes,
                DurationText = c.DurationText,
                Level = c.Level,
                InstructorName = c.InstructorName,
                TotalEpisodesCount = c.TotalEpisodesCount,
                AverageRating = c.AverageRating,
                RatingCount = c.RatingCount,
                StudentsCount = c.StudentsCount,
                IsFeatured = c.IsFeatured
            })
            .ToListAsync(cancellationToken);

        return PagedResult<CourseListDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}

public record GetCourseBySlugQuery(string Slug) : IRequest<CourseDto>;

public class GetCourseBySlugQueryHandler : IRequestHandler<GetCourseBySlugQuery, CourseDto>
{
    private readonly IApplicationDbContext _context;

    public GetCourseBySlugQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CourseDto> Handle(GetCourseBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLower();

        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Sections.OrderBy(s => s.DisplayOrder))
                .ThenInclude(s => s.Episodes.OrderBy(e => e.DisplayOrder))
            .Include(c => c.Episodes.OrderBy(e => e.DisplayOrder))
            .FirstOrDefaultAsync(c => c.Slug.ToLower() == slug, cancellationToken);

        if (course == null || !course.IsPublished)
            throw new NotFoundException("Course", request.Slug);

        // Helper to split stored newline/semicolon/pipe lists
        static List<string> ParseLines(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return [];
            return raw.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return new CourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Slug = course.Slug,
            Subtitle = course.Subtitle,
            Description = course.Description,
            Price = course.Price,
            CompareAtPrice = course.CompareAtPrice,
            CoverImageUrl = course.CoverImageUrl,
            IntroVideoUrl = course.IntroVideoUrl,
            FileServerBaseUrl = course.FileServerBaseUrl,
            StoragePath = course.StoragePath,
            DownloadableFilesUrl = course.DownloadableFilesUrl,
            DurationMinutes = course.DurationMinutes,
            DurationText = course.DurationText,
            Level = course.Level,
            Language = course.Language,
            TotalEpisodesCount = course.TotalEpisodesCount > 0 ? course.TotalEpisodesCount : course.Episodes.Count,
            DownloadableResourcesCount = course.DownloadableResourcesCount,
            IncludesCertificate = course.IncludesCertificate,
            HasLifetimeAccess = course.HasLifetimeAccess,
            InstructorName = course.InstructorName,
            InstructorTitle = course.InstructorTitle,
            InstructorBio = course.InstructorBio,
            InstructorAvatarUrl = course.InstructorAvatarUrl,
            AverageRating = course.AverageRating,
            RatingCount = course.RatingCount,
            StudentsCount = course.StudentsCount,
            IsFeatured = course.IsFeatured,
            WhatYouWillLearnList = ParseLines(course.WhatYouWillLearn),
            RequirementsList = ParseLines(course.Requirements),
            TargetAudienceList = ParseLines(course.TargetAudience),
            Sections = course.Sections.Select(s => new CourseSectionDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                Episodes = s.Episodes.Select(e => new CourseEpisodeDto
                {
                    Id = e.Id,
                    SectionId = e.SectionId,
                    Title = e.Title,
                    Description = e.Description,
                    DurationMinutes = e.DurationMinutes,
                    DurationText = e.DurationText,
                    VideoUrl = e.VideoUrl,
                    FileServerUrl = e.FileServerUrl,
                    AttachmentUrl = e.AttachmentUrl,
                    AttachmentName = e.AttachmentName,
                    AttachmentSizeBytes = e.AttachmentSizeBytes,
                    IsFreePreview = e.IsFreePreview,
                    DisplayOrder = e.DisplayOrder
                }).ToList()
            }).ToList(),
            SampleEpisodes = course.Episodes.Where(e => e.IsFreePreview).Select(e => new CourseEpisodeDto
            {
                Id = e.Id,
                SectionId = e.SectionId,
                Title = e.Title,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes,
                DurationText = e.DurationText,
                VideoUrl = e.VideoUrl,
                FileServerUrl = e.FileServerUrl,
                AttachmentUrl = e.AttachmentUrl,
                AttachmentName = e.AttachmentName,
                AttachmentSizeBytes = e.AttachmentSizeBytes,
                IsFreePreview = e.IsFreePreview,
                DisplayOrder = e.DisplayOrder
            }).ToList()
        };
    }
}

public record GetFeaturedCoursesQuery(int Count = 3) : IRequest<List<CourseListDto>>;

public class GetFeaturedCoursesQueryHandler : IRequestHandler<GetFeaturedCoursesQuery, List<CourseListDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFeaturedCoursesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CourseListDto>> Handle(GetFeaturedCoursesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && c.IsFeatured)
            .OrderBy(c => c.DisplayOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Take(request.Count)
            .Select(c => new CourseListDto
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                Subtitle = c.Subtitle,
                Price = c.Price,
                CompareAtPrice = c.CompareAtPrice,
                CoverImageUrl = c.CoverImageUrl,
                IntroVideoUrl = c.IntroVideoUrl,
                DurationMinutes = c.DurationMinutes,
                DurationText = c.DurationText,
                Level = c.Level,
                InstructorName = c.InstructorName,
                TotalEpisodesCount = c.TotalEpisodesCount,
                AverageRating = c.AverageRating,
                RatingCount = c.RatingCount,
                StudentsCount = c.StudentsCount,
                IsFeatured = c.IsFeatured
            })
            .ToListAsync(cancellationToken);
    }
}
