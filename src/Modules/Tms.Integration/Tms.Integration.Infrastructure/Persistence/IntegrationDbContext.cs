using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Integration.Infrastructure.Persistence;

public sealed class IntegrationDbContext(
    DbContextOptions<IntegrationDbContext> options,
    IPublisher publisher) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<OmsOrderSync> OmsOrderSyncs => Set<OmsOrderSync>();

    public DbSet<OmsFieldMapping> OmsFieldMappings => Set<OmsFieldMapping>();

    public DbSet<OmsOutboxEvent> OmsOutboxEvents => Set<OmsOutboxEvent>();

    public DbSet<AmrHandoffRecord> AmrHandoffRecords => Set<AmrHandoffRecord>();

    public DbSet<DockStation> DockStations => Set<DockStation>();

    public DbSet<ErpExportJob> ErpExportJobs => Set<ErpExportJob>();

    public DbSet<ErpExportRecord> ErpExportRecords => Set<ErpExportRecord>();

    public DbSet<ErpReconciliation> ErpReconciliations => Set<ErpReconciliation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("itg");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "itg");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


