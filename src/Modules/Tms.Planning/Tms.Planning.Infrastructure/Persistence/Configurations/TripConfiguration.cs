using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Infrastructure.Persistence.Configurations;

public sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TripNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.TripNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PlannedDate);
        builder.HasIndex(x => x.VehicleId);
        builder.HasIndex(x => x.DriverId);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TotalWeight).HasPrecision(12, 2);
        builder.Property(x => x.TotalVolumeCBM).HasPrecision(12, 4);
        builder.Property(x => x.TotalDistanceKm).HasPrecision(10, 2);
        builder.Property(x => x.CancelReason).HasMaxLength(500);

        // EF convention finds private field _stops automatically via property name Stops
        builder.HasMany<Stop>(t => t.Stops)
            .WithOne()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Stops)
            .HasField("_stops")
            .UsePropertyAccessMode(PropertyAccessMode.PreferField)
            .AutoInclude();
    }
}

public sealed class StopConfiguration : IEntityTypeConfiguration<Stop>
{
    public void Configure(EntityTypeBuilder<Stop> builder)
    {
        builder.ToTable("Stops");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.TripId, x.Sequence }).IsUnique();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AddressName).HasMaxLength(200);
        builder.Property(x => x.AddressStreet).HasMaxLength(500);
        builder.Property(x => x.AddressProvince).HasMaxLength(100);
        builder.Ignore(x => x.DomainEvents);
    }
}
