using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<Stop> Stops => Set<Stop>();

    public DbSet<RoutePlan> RoutePlans => Set<RoutePlan>();

    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    public DbSet<OptimizationRequest> OptimizationRequests => Set<OptimizationRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pln");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "pln");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


