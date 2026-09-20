using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class ProductCommentConfiguration : IEntityTypeConfiguration<ProductComment>
{
    public void Configure(EntityTypeBuilder<ProductComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.AuthorName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.AuthorEmail)
            .HasMaxLength(200);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(c => c.AdminReply)
            .HasMaxLength(2000);

        builder.Property(c => c.Rating)
            .HasDefaultValue(5);

        builder.Property(c => c.IsApproved)
            .HasDefaultValue(false);

        builder.HasIndex(c => c.ProductId);
        builder.HasIndex(c => c.IsApproved);
        builder.HasIndex(c => c.CreatedAt);

        builder.HasOne(c => c.Product)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
