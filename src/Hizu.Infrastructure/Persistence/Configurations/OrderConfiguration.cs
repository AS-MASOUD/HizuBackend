using Hizu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hizu.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique();

        builder.Property(o => o.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(o => o.ShippingCost)
            .HasPrecision(18, 2);

        builder.Property(o => o.Total)
            .HasPrecision(18, 2);

        builder.OwnsOne(o => o.ShippingAddress, a =>
        {
            a.Property(p => p.Street).HasMaxLength(300).HasColumnName("ShippingStreet");
            a.Property(p => p.City).HasMaxLength(100).HasColumnName("ShippingCity");
            a.Property(p => p.State).HasMaxLength(100).HasColumnName("ShippingState");
            a.Property(p => p.ZipCode).HasMaxLength(20).HasColumnName("ShippingZipCode");
            a.Property(p => p.Country).HasMaxLength(50).HasColumnName("ShippingCountry");
        });

        builder.HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(oi => oi.Sku)
            .HasMaxLength(50);

        builder.Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(oi => oi.TotalPrice)
            .HasPrecision(18, 2);

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
