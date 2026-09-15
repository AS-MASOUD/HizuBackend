using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.OwnsOne(u => u.Address, a =>
        {
            a.Property(p => p.Street).HasMaxLength(300).HasColumnName("AddressStreet");
            a.Property(p => p.City).HasMaxLength(100).HasColumnName("AddressCity");
            a.Property(p => p.State).HasMaxLength(100).HasColumnName("AddressState");
            a.Property(p => p.ZipCode).HasMaxLength(20).HasColumnName("AddressZipCode");
            a.Property(p => p.Country).HasMaxLength(50).HasColumnName("AddressCountry");
        });
    }
}
