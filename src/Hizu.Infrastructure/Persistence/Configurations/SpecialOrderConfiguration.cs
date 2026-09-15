using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class SpecialOrderConfiguration : IEntityTypeConfiguration<SpecialOrder>
{
    public void Configure(EntityTypeBuilder<SpecialOrder> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.OrderNumber)
            .IsUnique();

        builder.Property(s => s.CustomerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.CustomerPhone)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.CustomerEmail)
            .HasMaxLength(150);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Category)
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(s => s.MaterialPreference)
            .HasMaxLength(150);

        builder.Property(s => s.EstimatedBudget)
            .HasPrecision(18, 2);

        builder.Property(s => s.QuotedPrice)
            .HasPrecision(18, 2);

        builder.Property(s => s.AdminReply)
            .HasMaxLength(2000);

        builder.HasOne(s => s.User)
            .WithMany(u => u.SpecialOrders)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SpecialOrderImageConfiguration : IEntityTypeConfiguration<SpecialOrderImage>
{
    public void Configure(EntityTypeBuilder<SpecialOrderImage> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.OriginalFileName)
            .HasMaxLength(255);

        builder.HasOne(i => i.SpecialOrder)
            .WithMany(s => s.Images)
            .HasForeignKey(i => i.SpecialOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
