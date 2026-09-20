using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Slug)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(a => a.Slug)
            .IsUnique();

        builder.Property(a => a.Summary)
            .IsRequired()
            .HasMaxLength(1500);

        builder.Property(a => a.Content)
            .IsRequired();

        builder.Property(a => a.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.CoverImage)
            .HasMaxLength(1000);

        builder.Property(a => a.Author)
            .HasMaxLength(150);

        builder.Property(a => a.AuthorRole)
            .HasMaxLength(150);

        builder.Property(a => a.Tags)
            .HasMaxLength(500);
    }
}
