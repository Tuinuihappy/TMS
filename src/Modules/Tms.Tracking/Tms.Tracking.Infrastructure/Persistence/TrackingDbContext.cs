using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Application;
using Tms.Tracking.Domain.Entities;

namespace Tms.Tracking.Infrastructure.Persistence;

public sealed class TrackingDbContext(DbContextOptions<TrackingDbContext> options, IPublisher publisher)
    : DbContext(options)
{
    public DbSet<VehiclePosition> VehiclePositions => Set<VehiclePosition>();
    public DbSet<CurrentVehicleState> CurrentVehicleStates => Set<CurrentVehicleState>();
    public DbSet<GeoZone> GeoZones => Set<GeoZone>();
    public DbSet<ZoneEvent> ZoneEvents => Set<ZoneEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("trk");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrackingDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.DispatchDomainEventsAsync(this, publisher, cancellationToken);
        return result;
    }
}
