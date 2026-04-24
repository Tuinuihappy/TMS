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

        builder.Property(x => x.TenantId).IsRequired();

        builder.OwnsOne(x => x.PickupAddress, a =>
        {
            a.Property(p => p.Name).HasColumnName("PickupAddress_Name").HasMaxLength(200);
            a.Property(p => p.Street).HasColumnName("PickupAddress_Street").HasMaxLength(200);
            a.Property(p => p.SubDistrict).HasColumnName("PickupAddress_SubDistrict").HasMaxLength(100);
            a.Property(p => p.District).HasColumnName("PickupAddress_District").HasMaxLength(100);
            a.Property(p => p.Province).HasColumnName("PickupAddress_Province").HasMaxLength(100);
            a.Property(p => p.PostalCode).HasColumnName("PickupAddress_PostalCode").HasMaxLength(10);
            a.Property(p => p.Latitude).HasColumnName("PickupAddress_Latitude");
            a.Property(p => p.Longitude).HasColumnName("PickupAddress_Longitude");
        });

        builder.OwnsOne(x => x.DropoffAddress, a =>
        {
            a.Property(p => p.Name).HasColumnName("DropoffAddress_Name").HasMaxLength(200);
            a.Property(p => p.Street).HasColumnName("DropoffAddress_Street").HasMaxLength(200);
            a.Property(p => p.SubDistrict).HasColumnName("DropoffAddress_SubDistrict").HasMaxLength(100);
            a.Property(p => p.District).HasColumnName("DropoffAddress_District").HasMaxLength(100);
            a.Property(p => p.Province).HasColumnName("DropoffAddress_Province").HasMaxLength(100);
            a.Property(p => p.PostalCode).HasColumnName("DropoffAddress_PostalCode").HasMaxLength(10);
            a.Property(p => p.Latitude).HasColumnName("DropoffAddress_Latitude");
            a.Property(p => p.Longitude).HasColumnName("DropoffAddress_Longitude");
        });

        builder.OwnsOne(x => x.PickupWindow, w =>
        {
            w.Property(p => p.From).HasColumnName("PickupWindowFrom");
            w.Property(p => p.To).HasColumnName("PickupWindowTo");
        });

        builder.OwnsOne(x => x.DropoffWindow, w =>
        {
            w.Property(p => p.From).HasColumnName("DropoffWindowFrom");
            w.Property(p => p.To).HasColumnName("DropoffWindowTo");
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

        builder.OwnsOne(x => x.Weight, w =>
        {
            w.Property(p => p.Value).HasColumnName("WeightKg").HasPrecision(10, 3);
            w.Property(p => p.Unit)
                .HasConversion<string>()
                .HasColumnName("WeightUnit")
                .HasMaxLength(5);
        });

        builder.Property(x => x.VolumeCBM).HasPrecision(10, 3);
    }
}
