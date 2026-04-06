using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Application;
using Tms.Tracking.Domain.Entities;

namespace Tms.Tracking.Infrastructure.Persistence;

public sealed class TrackingDbContext(DbContextOptions<TrackingDbContext> options)
    : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
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

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "trk");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


