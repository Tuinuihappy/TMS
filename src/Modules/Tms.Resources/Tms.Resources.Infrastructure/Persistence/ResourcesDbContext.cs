using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Resources.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : DbContext(options)
{
    public DbSet<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage> OutboxMessages => Set<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    public DbSet<InsuranceRecord> InsuranceRecords => Set<InsuranceRecord>();

    public DbSet<Driver> Drivers => Set<Driver>();

    public DbSet<HOSRecord> HOSRecords => Set<HOSRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("res");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourcesDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
            .Where(e => typeof(Tms.SharedKernel.Domain.AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var versionProp = entityType.FindProperty("Version");
            if (versionProp is not null)
                versionProp.IsConcurrencyToken = false;
        }

        modelBuilder.Entity<Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage>().ToTable("OutboxMessages", "res");
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DomainEventDispatcher.StoreDomainEventsInOutbox(this);
        return await base.SaveChangesAsync(cancellationToken);
    }
}


