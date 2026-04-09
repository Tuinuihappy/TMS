using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Tracking.Domain.Entities;

namespace Tms.Tracking.Infrastructure.Persistence.Configurations;

internal sealed class VehiclePositionConfiguration : IEntityTypeConfiguration<VehiclePosition>
{
    public void Configure(EntityTypeBuilder<VehiclePosition> builder)
    {
        builder.ToTable("VehiclePositions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VehicleId).IsRequired();
        builder.Property(x => x.Latitude).IsRequired();
        builder.Property(x => x.Longitude).IsRequired();
        builder.Property(x => x.SpeedKmh).HasPrecision(5, 2);
        builder.Property(x => x.Heading).HasPrecision(5, 2);
        builder.Property(x => x.Timestamp).IsRequired();

        builder.HasIndex(x => new { x.VehicleId, x.Timestamp });
        builder.HasIndex(x => x.TripId);
    }
}

internal sealed class CurrentVehicleStateConfiguration : IEntityTypeConfiguration<CurrentVehicleState>
{
    public void Configure(EntityTypeBuilder<CurrentVehicleState> builder)
    {
        builder.ToTable("CurrentVehicleStates");
        builder.HasKey(x => x.VehicleId);
        builder.Property(x => x.Latitude).IsRequired();
        builder.Property(x => x.Longitude).IsRequired();
        builder.Property(x => x.SpeedKmh).HasPrecision(5, 2);
        builder.Property(x => x.LastUpdatedAt).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
    }
}

internal sealed class GeoZoneConfiguration : IEntityTypeConfiguration<GeoZone>
{
    public void Configure(EntityTypeBuilder<GeoZone> builder)
    {
        builder.ToTable("GeoZones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RadiusMeters);
        builder.Property(x => x.CenterLatitude);
        builder.Property(x => x.CenterLongitude);
        builder.Property(x => x.PolygonCoordinatesJson).HasColumnType("jsonb");
        builder.Property(x => x.IsActive).IsRequired();
        // "Pickup" | "Dropoff" | null — แยก logic ของ Geofence handler
        builder.Property(x => x.StopType).HasMaxLength(20);

        builder.HasMany<ZoneEvent>()
            .WithOne()
            .HasForeignKey(ze => ze.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ZoneEventConfiguration : IEntityTypeConfiguration<ZoneEvent>
{
    public void Configure(EntityTypeBuilder<ZoneEvent> builder)
    {
        builder.ToTable("ZoneEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ZoneId).IsRequired();
        builder.Property(x => x.VehicleId).IsRequired();
        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();

        builder.HasIndex(x => new { x.VehicleId, x.Timestamp });
    }
}
