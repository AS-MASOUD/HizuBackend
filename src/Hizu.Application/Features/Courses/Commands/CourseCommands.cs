using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Helpers;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Courses.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Courses.Commands;

public record CreateCourseCommand(UpsertCourseDto CourseData) : IRequest<CourseDto>;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.CourseData.Title)
            .NotEmpty().WithMessage("عنوان دوره آموزشی الزامی است.")
            .MaximumLength(250).WithMessage("عنوان دوره نمی‌تواند بیش از ۲۵۰ کاراکتر باشد.");

        RuleFor(x => x.CourseData.Description)
            .NotEmpty().WithMessage("توضیحات دوره آموزشی الزامی است.");

        RuleFor(x => x.CourseData.InstructorName)
            .NotEmpty().WithMessage("نام مدرس الزامی است.")
            .MaximumLength(150);
    }
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var data = request.CourseData;

        // Generate or clean slug
        var slug = string.IsNullOrWhiteSpace(data.Slug)
            ? SlugHelper.GenerateSlug(data.Title)
            : SlugHelper.GenerateSlug(data.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"course-{Guid.NewGuid().ToString("N")[..8]}";
        }

        // Ensure unique slug
        var baseSlug = slug;
        var counter = 1;
        while (await _context.Courses.AnyAsync(c => c.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        // If price is null or 0, or IsReservationOnly is checked, treat as reservation
        var isReservation = data.IsReservationOnly || !data.Price.HasValue || data.Price.Value <= 0;
        var price = isReservation ? (decimal?)null : data.Price;

        var course = new Course
        {
            Title = data.Title.Trim(),
            Slug = slug,
            Subtitle = data.Subtitle?.Trim(),
            Description = data.Description.Trim(),
            Price = price,
            CompareAtPrice = isReservation ? null : data.CompareAtPrice,
            IsReservationOnly = isReservation,
            CoverImageUrl = data.CoverImageUrl?.Trim(),
            IntroVideoUrl = data.IntroVideoUrl?.Trim(),
            FileServerBaseUrl = data.FileServerBaseUrl?.Trim(),
            StoragePath = data.StoragePath?.Trim(),
            DownloadableFilesUrl = data.DownloadableFilesUrl?.Trim(),
            DurationMinutes = data.DurationMinutes,
            DurationText = data.DurationText?.Trim(),
            Level = data.Level,
            Language = string.IsNullOrWhiteSpace(data.Language) ? "فارسی" : data.Language.Trim(),
            TotalEpisodesCount = data.TotalEpisodesCount,
            DownloadableResourcesCount = data.DownloadableResourcesCount,
            IncludesCertificate = data.IncludesCertificate,
            HasLifetimeAccess = data.HasLifetimeAccess,
            InstructorName = string.IsNullOrWhiteSpace(data.InstructorName) ? "تیم فنی مهندسی هیزو" : data.InstructorName.Trim(),
            InstructorTitle = data.InstructorTitle?.Trim(),
            InstructorBio = data.InstructorBio?.Trim(),
            InstructorAvatarUrl = data.InstructorAvatarUrl?.Trim(),
            WhatYouWillLearn = data.WhatYouWillLearn?.Trim(),
            Requirements = data.Requirements?.Trim(),
            TargetAudience = data.TargetAudience?.Trim(),
            IsPublished = data.IsPublished,
            IsFeatured = data.IsFeatured,
            DisplayOrder = data.DisplayOrder,
            AverageRating = 5.0m,
            RatingCount = 0,
            StudentsCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        if (data.Sections != null && data.Sections.Count > 0)
        {
            var secOrder = 1;
            foreach (var secDto in data.Sections)
            {
                var section = new CourseSection
                {
                    CourseId = course.Id,
                    Title = secDto.Title.Trim(),
                    Description = secDto.Description?.Trim(),
                    DisplayOrder = secDto.DisplayOrder > 0 ? secDto.DisplayOrder : secOrder++
                };

                var epOrder = 1;
                foreach (var epDto in secDto.Episodes)
                {
                    section.Episodes.Add(new CourseEpisode
                    {
                        CourseId = course.Id,
                        Title = epDto.Title.Trim(),
                        Description = epDto.Description?.Trim(),
                        DurationMinutes = epDto.DurationMinutes,
                        DurationText = epDto.DurationText?.Trim(),
                        VideoUrl = epDto.VideoUrl?.Trim(),
                        FileServerUrl = epDto.FileServerUrl?.Trim(),
                        AttachmentUrl = epDto.AttachmentUrl?.Trim(),
                        AttachmentName = epDto.AttachmentName?.Trim(),
                        AttachmentSizeBytes = epDto.AttachmentSizeBytes,
                        IsFreePreview = epDto.IsFreePreview,
                        DisplayOrder = epDto.DisplayOrder > 0 ? epDto.DisplayOrder : epOrder++
                    });
                }
                course.Sections.Add(section);
            }

            var allEps = course.Sections.SelectMany(s => s.Episodes).ToList();
            course.TotalEpisodesCount = allEps.Count;
            if (course.DurationMinutes <= 0)
            {
                course.DurationMinutes = allEps.Sum(e => e.DurationMinutes);
            }
        }

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

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
            IsReservationOnly = course.IsReservationOnly,
            CoverImageUrl = course.CoverImageUrl,
            IntroVideoUrl = course.IntroVideoUrl,
            FileServerBaseUrl = course.FileServerBaseUrl,
            StoragePath = course.StoragePath,
            DownloadableFilesUrl = course.DownloadableFilesUrl,
            DurationMinutes = course.DurationMinutes,
            DurationText = course.DurationText,
            Level = course.Level,
            Language = course.Language,
            TotalEpisodesCount = course.TotalEpisodesCount,
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
            IsPublished = course.IsPublished,
            IsFeatured = course.IsFeatured,
            DisplayOrder = course.DisplayOrder,
            CreatedAt = course.CreatedAt,
            WhatYouWillLearn = course.WhatYouWillLearn,
            Requirements = course.Requirements,
            TargetAudience = course.TargetAudience,
            WhatYouWillLearnList = ParseLines(course.WhatYouWillLearn),
            RequirementsList = ParseLines(course.Requirements),
            TargetAudienceList = ParseLines(course.TargetAudience),
            Sections = course.Sections.OrderBy(s => s.DisplayOrder).Select(s => new CourseSectionDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                Episodes = s.Episodes.OrderBy(e => e.DisplayOrder).Select(e => new CourseEpisodeDto
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
            }).ToList()
        };
    }
}

public record UpdateCourseCommand(Guid Id, UpsertCourseDto CourseData) : IRequest<CourseDto>;

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CourseDto> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.Sections)
                .ThenInclude(s => s.Episodes)
            .Include(c => c.Episodes)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course == null)
            throw new NotFoundException("Course", request.Id);

        var data = request.CourseData;

        // Slug management
        if (!string.IsNullOrWhiteSpace(data.Slug))
        {
            var newSlug = SlugHelper.GenerateSlug(data.Slug);
            if (!string.IsNullOrWhiteSpace(newSlug) && newSlug != course.Slug)
            {
                if (await _context.Courses.AnyAsync(c => c.Slug == newSlug && c.Id != course.Id, cancellationToken))
                    throw new BadRequestException($"نامک «{newSlug}» قبلاً برای دوره دیگری استفاده شده است.");

                course.Slug = newSlug;
            }
        }

        // Reservation & Price handling:
        // If price is cleared/empty or IsReservationOnly is checked, enable reservation
        var isReservation = data.IsReservationOnly || !data.Price.HasValue || data.Price.Value <= 0;
        var price = isReservation ? (decimal?)null : data.Price;

        course.Title = data.Title.Trim();
        course.Subtitle = data.Subtitle?.Trim();
        course.Description = data.Description.Trim();
        course.Price = price;
        course.CompareAtPrice = isReservation ? null : data.CompareAtPrice;
        course.IsReservationOnly = isReservation;
        course.CoverImageUrl = data.CoverImageUrl?.Trim();
        course.IntroVideoUrl = data.IntroVideoUrl?.Trim();
        course.FileServerBaseUrl = data.FileServerBaseUrl?.Trim();
        course.StoragePath = data.StoragePath?.Trim();
        course.DownloadableFilesUrl = data.DownloadableFilesUrl?.Trim();
        course.DurationMinutes = data.DurationMinutes;
        course.DurationText = data.DurationText?.Trim();
        course.Level = data.Level;
        course.Language = string.IsNullOrWhiteSpace(data.Language) ? "فارسی" : data.Language.Trim();
        course.TotalEpisodesCount = data.TotalEpisodesCount;
        course.DownloadableResourcesCount = data.DownloadableResourcesCount;
        course.IncludesCertificate = data.IncludesCertificate;
        course.HasLifetimeAccess = data.HasLifetimeAccess;
        course.InstructorName = string.IsNullOrWhiteSpace(data.InstructorName) ? "تیم فنی مهندسی هیزو" : data.InstructorName.Trim();
        course.InstructorTitle = data.InstructorTitle?.Trim();
        course.InstructorBio = data.InstructorBio?.Trim();
        course.InstructorAvatarUrl = data.InstructorAvatarUrl?.Trim();
        course.WhatYouWillLearn = data.WhatYouWillLearn?.Trim();
        course.Requirements = data.Requirements?.Trim();
        course.TargetAudience = data.TargetAudience?.Trim();
        course.IsPublished = data.IsPublished;
        course.IsFeatured = data.IsFeatured;
        course.DisplayOrder = data.DisplayOrder;
        course.UpdatedAt = DateTime.UtcNow;

        if (data.Sections != null)
        {
            var incomingSectionIds = data.Sections.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();

            // Remove sections that were removed in the editor
            var sectionsToRemove = course.Sections.Where(s => !incomingSectionIds.Contains(s.Id)).ToList();
            foreach (var sec in sectionsToRemove)
            {
                _context.CourseEpisodes.RemoveRange(sec.Episodes);
                _context.CourseSections.Remove(sec);
                course.Sections.Remove(sec);
            }

            var secOrder = 1;
            foreach (var secDto in data.Sections)
            {
                CourseSection section;
                if (secDto.Id != Guid.Empty && course.Sections.FirstOrDefault(s => s.Id == secDto.Id) is { } existingSec)
                {
                    section = existingSec;
                    section.Title = secDto.Title.Trim();
                    section.Description = secDto.Description?.Trim();
                    section.DisplayOrder = secDto.DisplayOrder > 0 ? secDto.DisplayOrder : secOrder++;
                }
                else
                {
                    section = new CourseSection
                    {
                        CourseId = course.Id,
                        Title = secDto.Title.Trim(),
                        Description = secDto.Description?.Trim(),
                        DisplayOrder = secDto.DisplayOrder > 0 ? secDto.DisplayOrder : secOrder++
                    };
                    course.Sections.Add(section);
                }

                // Sync episodes for this section
                var incomingEpIds = secDto.Episodes.Where(e => e.Id != Guid.Empty).Select(e => e.Id).ToHashSet();
                var epsToRemove = section.Episodes.Where(e => !incomingEpIds.Contains(e.Id)).ToList();
                foreach (var ep in epsToRemove)
                {
                    _context.CourseEpisodes.Remove(ep);
                    section.Episodes.Remove(ep);
                }

                var epOrder = 1;
                foreach (var epDto in secDto.Episodes)
                {
                    if (epDto.Id != Guid.Empty && section.Episodes.FirstOrDefault(e => e.Id == epDto.Id) is { } existingEp)
                    {
                        existingEp.Title = epDto.Title.Trim();
                        existingEp.Description = epDto.Description?.Trim();
                        existingEp.DurationMinutes = epDto.DurationMinutes;
                        existingEp.DurationText = epDto.DurationText?.Trim();
                        existingEp.VideoUrl = epDto.VideoUrl?.Trim();
                        existingEp.FileServerUrl = epDto.FileServerUrl?.Trim();
                        existingEp.AttachmentUrl = epDto.AttachmentUrl?.Trim();
                        existingEp.AttachmentName = epDto.AttachmentName?.Trim();
                        existingEp.AttachmentSizeBytes = epDto.AttachmentSizeBytes;
                        existingEp.IsFreePreview = epDto.IsFreePreview;
                        existingEp.DisplayOrder = epDto.DisplayOrder > 0 ? epDto.DisplayOrder : epOrder++;
                    }
                    else
                    {
                        section.Episodes.Add(new CourseEpisode
                        {
                            CourseId = course.Id,
                            SectionId = section.Id,
                            Title = epDto.Title.Trim(),
                            Description = epDto.Description?.Trim(),
                            DurationMinutes = epDto.DurationMinutes,
                            DurationText = epDto.DurationText?.Trim(),
                            VideoUrl = epDto.VideoUrl?.Trim(),
                            FileServerUrl = epDto.FileServerUrl?.Trim(),
                            AttachmentUrl = epDto.AttachmentUrl?.Trim(),
                            AttachmentName = epDto.AttachmentName?.Trim(),
                            AttachmentSizeBytes = epDto.AttachmentSizeBytes,
                            IsFreePreview = epDto.IsFreePreview,
                            DisplayOrder = epDto.DisplayOrder > 0 ? epDto.DisplayOrder : epOrder++
                        });
                    }
                }
            }

            var allEps = course.Sections.SelectMany(s => s.Episodes).ToList();
            course.TotalEpisodesCount = allEps.Count;
            if (data.DurationMinutes > 0)
            {
                course.DurationMinutes = data.DurationMinutes;
            }
            else
            {
                course.DurationMinutes = allEps.Sum(e => e.DurationMinutes);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

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
            IsReservationOnly = course.IsReservationOnly,
            CoverImageUrl = course.CoverImageUrl,
            IntroVideoUrl = course.IntroVideoUrl,
            FileServerBaseUrl = course.FileServerBaseUrl,
            StoragePath = course.StoragePath,
            DownloadableFilesUrl = course.DownloadableFilesUrl,
            DurationMinutes = course.DurationMinutes,
            DurationText = course.DurationText,
            Level = course.Level,
            Language = course.Language,
            TotalEpisodesCount = course.TotalEpisodesCount,
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
            IsPublished = course.IsPublished,
            IsFeatured = course.IsFeatured,
            DisplayOrder = course.DisplayOrder,
            CreatedAt = course.CreatedAt,
            WhatYouWillLearn = course.WhatYouWillLearn,
            Requirements = course.Requirements,
            TargetAudience = course.TargetAudience,
            WhatYouWillLearnList = ParseLines(course.WhatYouWillLearn),
            RequirementsList = ParseLines(course.Requirements),
            TargetAudienceList = ParseLines(course.TargetAudience),
            Sections = course.Sections.OrderBy(s => s.DisplayOrder).Select(s => new CourseSectionDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                Episodes = s.Episodes.OrderBy(e => e.DisplayOrder).Select(e => new CourseEpisodeDto
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
            }).ToList()
        };
    }
}

public record DeleteCourseCommand(Guid Id) : IRequest<bool>;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Include(c => c.Sections)
                .ThenInclude(s => s.Episodes)
            .Include(c => c.Episodes)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course == null)
            throw new NotFoundException("Course", request.Id);

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public record ToggleCoursePublishCommand(Guid Id) : IRequest<bool>;

public class ToggleCoursePublishCommandHandler : IRequestHandler<ToggleCoursePublishCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ToggleCoursePublishCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ToggleCoursePublishCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses.FindAsync([request.Id], cancellationToken);
        if (course == null)
            throw new NotFoundException("Course", request.Id);

        course.IsPublished = !course.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return course.IsPublished;
    }
}

public record ReserveCourseCommand(Guid CourseId, Guid UserId) : IRequest<bool>;

public class ReserveCourseCommandHandler : IRequestHandler<ReserveCourseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ReserveCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ReserveCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses.FindAsync([request.CourseId], cancellationToken);
        if (course == null || !course.IsPublished)
            throw new NotFoundException("Course", request.CourseId);

        var user = await _context.Users.FindAsync([request.UserId], cancellationToken);
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        // Check if user already enrolled / reserved
        var existing = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.UserId == request.UserId, cancellationToken);

        if (existing != null)
        {
            // Already enrolled or reserved
            return true;
        }

        // Generate Order record for the course reservation so it appears on the Admin Panel Dashboard
        var orderCount = await _context.Orders.CountAsync(cancellationToken);
        var orderNumber = $"ORD-CRS-{DateTime.UtcNow:yyMMdd}-{orderCount + 1:D3}";

        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = request.UserId,
            Status = OrderStatus.Processing,
            ShippingAddress = user.Address ?? new Address
            {
                City = "تهران",
                State = "تهران",
                Street = "پیش‌ثبت‌نام آنلاین دوره آموزشی کارگاهی",
                ZipCode = "0000000000",
                Country = "ایران"
            },
            Subtotal = 0,
            ShippingCost = 0,
            Total = 0,
            IsStockDeducted = true,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    CourseId = course.Id,
                    ProductId = null,
                    ProductName = $"[دوره] {course.Title}",
                    Sku = $"CRS-{course.Slug.ToUpper()}",
                    Quantity = 1,
                    UnitPrice = 0,
                    TotalPrice = 0,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _context.Orders.Add(order);

        var enrollment = new CourseEnrollment
        {
            CourseId = course.Id,
            UserId = request.UserId,
            OrderId = order.Id,
            EnrolledAt = DateTime.UtcNow,
            ProgressPercentage = 0,
            IsCompleted = false
        };

        _context.CourseEnrollments.Add(enrollment);
        course.StudentsCount += 1;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
