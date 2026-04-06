using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Execution.Infrastructure.Persistence;

public sealed class ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();

    public DbSet<PODRecord> PODRecords => Set<PODRecord>();

    public DbSet<PODPhoto> PODPhotos => Set<PODPhoto>();

    public DbSet<PODDocument> PODDocuments => Set<PODDocument>();

    public DbSet<VerificationItem> VerificationItems => Set<VerificationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("exe");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExecutionDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "exe");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


