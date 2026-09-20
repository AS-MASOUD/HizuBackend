using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Subtitle)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .IsRequired();

        builder.Property(c => c.Price)
            .HasPrecision(18, 2);

        builder.Property(c => c.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(c => c.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.IntroVideoUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.FileServerBaseUrl)
            .HasMaxLength(500);

        builder.Property(c => c.StoragePath)
            .HasMaxLength(500);

        builder.Property(c => c.DownloadableFilesUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.Language)
            .HasMaxLength(50)
            .HasDefaultValue("فارسی");

        builder.Property(c => c.DurationText)
            .HasMaxLength(100);

        builder.Property(c => c.InstructorName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.InstructorTitle)
            .HasMaxLength(200);

        builder.Property(c => c.InstructorAvatarUrl)
            .HasMaxLength(1000);

        builder.Property(c => c.AverageRating)
            .HasPrecision(3, 2)
            .HasDefaultValue(5.0m);

        builder.HasMany(c => c.Sections)
            .WithOne(s => s.Course)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Episodes)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Enrollments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CourseSectionConfiguration : IEntityTypeConfiguration<CourseSection>
{
    public void Configure(EntityTypeBuilder<CourseSection> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.HasMany(s => s.Episodes)
            .WithOne(e => e.Section)
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class CourseEpisodeConfiguration : IEntityTypeConfiguration<CourseEpisode>
{
    public void Configure(EntityTypeBuilder<CourseEpisode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.DurationText)
            .HasMaxLength(50);

        builder.Property(e => e.VideoUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.FileServerUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.AttachmentUrl)
            .HasMaxLength(1000);

        builder.Property(e => e.AttachmentName)
            .HasMaxLength(250);
    }
}

public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.User)
            .WithMany(u => u.CourseEnrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique();
    }
}
