using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Infrastructure.Persistence.Configurations;

internal sealed class RoutePlanConfiguration : IEntityTypeConfiguration<RoutePlan>
{
    public void Configure(EntityTypeBuilder<RoutePlan> builder)
    {
        builder.ToTable("RoutePlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlanNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PlannedDate).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.TotalDistanceKm).HasPrecision(10, 2);
        builder.Property(x => x.CapacityUtilizationPercent).HasPrecision(5, 2);

        builder.HasMany(x => x.Stops)
            .WithOne()
            .HasForeignKey(s => s.RoutePlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
{
    public void Configure(EntityTypeBuilder<RouteStop> builder)
    {
        builder.ToTable("RouteStops");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoutePlanId).IsRequired();
        builder.Property(x => x.Sequence).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        // "Pickup" | "Dropoff" — required, default "Dropoff" for backward compat
        builder.Property(x => x.StopType).HasMaxLength(20).IsRequired()
            .HasDefaultValue("Dropoff");
        builder.Property(x => x.Latitude).IsRequired();
        builder.Property(x => x.Longitude).IsRequired();

        builder.HasIndex(x => x.RoutePlanId);
        builder.HasIndex(x => new { x.RoutePlanId, x.StopType });
    }
}

internal sealed class OptimizationRequestConfiguration : IEntityTypeConfiguration<OptimizationRequest>
{
    public void Configure(EntityTypeBuilder<OptimizationRequest> builder)
    {
        builder.ToTable("OptimizationRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ParametersJson).HasColumnType("jsonb");
        builder.Property(x => x.ResultDataJson).HasColumnType("jsonb");
        builder.Property(x => x.RequestedAt).IsRequired();

        // OptimizationRequest ↔ RoutePlan: one request → many plans
        builder.HasMany(x => x.Plans)
            .WithOne()
            .OnDelete(DeleteBehavior.SetNull);
    }
}
