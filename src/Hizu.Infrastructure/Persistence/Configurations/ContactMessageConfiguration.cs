using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(c => c.Subject)
            .HasMaxLength(250);

        builder.Property(c => c.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.AdminNotes)
            .HasMaxLength(3000);

        builder.Property(c => c.IsRead)
            .HasDefaultValue(false);

        builder.HasIndex(c => c.IsRead);
        builder.HasIndex(c => c.CreatedAt);
    }
}
