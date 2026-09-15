using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(c => c.Slug)
            .IsUnique();

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
