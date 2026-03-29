using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;

namespace Tms.Orders.Infrastructure.Persistence.Configurations;

public sealed class TransportOrderConfiguration : IEntityTypeConfiguration<TransportOrder>
{
    public void Configure(EntityTypeBuilder<TransportOrder> builder)
    {
        builder.ToTable("TransportOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber)
            .IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.OrderNumber).IsUnique();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsOne(x => x.PickupAddress, a =>
        {
            a.Property(p => p.Street).HasColumnName("PickupStreet").HasMaxLength(200);
            a.Property(p => p.SubDistrict).HasColumnName("PickupSubDistrict").HasMaxLength(100);
            a.Property(p => p.District).HasColumnName("PickupDistrict").HasMaxLength(100);
            a.Property(p => p.Province).HasColumnName("PickupProvince").HasMaxLength(100);
            a.Property(p => p.PostalCode).HasColumnName("PickupPostalCode").HasMaxLength(10);
            a.Property(p => p.Latitude).HasColumnName("PickupLat");
            a.Property(p => p.Longitude).HasColumnName("PickupLng");
        });

        builder.OwnsOne(x => x.DropoffAddress, a =>
        {
            a.Property(p => p.Street).HasColumnName("DropoffStreet").HasMaxLength(200);
            a.Property(p => p.SubDistrict).HasColumnName("DropoffSubDistrict").HasMaxLength(100);
            a.Property(p => p.District).HasColumnName("DropoffDistrict").HasMaxLength(100);
            a.Property(p => p.Province).HasColumnName("DropoffProvince").HasMaxLength(100);
            a.Property(p => p.PostalCode).HasColumnName("DropoffPostalCode").HasMaxLength(10);
            a.Property(p => p.Latitude).HasColumnName("DropoffLat");
            a.Property(p => p.Longitude).HasColumnName("DropoffLng");
        });

        builder.OwnsOne(x => x.PickupWindow, w =>
        {
            w.Property(p => p.From).HasColumnName("PickupFrom");
            w.Property(p => p.To).HasColumnName("PickupTo");
        });

        builder.OwnsOne(x => x.DropoffWindow, w =>
        {
            w.Property(p => p.From).HasColumnName("DropoffFrom");
            w.Property(p => p.To).HasColumnName("DropoffTo");
        });

        builder.HasMany<OrderItem>(o => o.Items)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude();
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Weight).HasPrecision(10, 3);
        builder.Property(x => x.Volume).HasPrecision(10, 3);
    }
}
