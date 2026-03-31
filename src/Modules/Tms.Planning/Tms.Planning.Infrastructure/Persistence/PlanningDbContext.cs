using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Stop> Stops => Set<Stop>();

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

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.DispatchDomainEventsAsync(this, publisher, cancellationToken);
        return result;
    }
}
