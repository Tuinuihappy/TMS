using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Infrastructure.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShipmentNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.ShipmentNumber).IsUnique();
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.AddressName).HasMaxLength(200);
        builder.Property(x => x.AddressStreet).HasMaxLength(500);
        builder.Property(x => x.AddressProvince).HasMaxLength(100);
        builder.Property(x => x.ExceptionReason).HasMaxLength(500);
        builder.Property(x => x.ExceptionReasonCode).HasMaxLength(20);

        // Items
        builder.HasMany<ShipmentItem>(s => s.Items)
            .WithOne()
            .HasForeignKey(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude();

        // POD
        builder.HasOne<PODRecord>(x => x.POD)
            .WithOne()
            .HasForeignKey<PODRecord>(x => x.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItems");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ShipmentId);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.SKU).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PODRecordConfiguration : IEntityTypeConfiguration<PODRecord>
{
    public void Configure(EntityTypeBuilder<PODRecord> builder)
    {
        builder.ToTable("PODRecords");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ShipmentId).IsUnique();
        builder.Property(x => x.ReceiverName).HasMaxLength(200);
        builder.Property(x => x.SignatureUrl).HasMaxLength(1000);
        builder.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(20);

        // Photos
        builder.HasMany<PODPhoto>(p => p.Photos)
            .WithOne()
            .HasForeignKey(x => x.PODRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Photos)
            .HasField("_photos")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude();
    }
}

public sealed class PODPhotoConfiguration : IEntityTypeConfiguration<PODPhoto>
{
    public void Configure(EntityTypeBuilder<PODPhoto> builder)
    {
        builder.ToTable("PODPhotos");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PODRecordId);
        builder.Property(x => x.PhotoUrl).IsRequired().HasMaxLength(1000);
        builder.Ignore(x => x.DomainEvents);
    }
}
