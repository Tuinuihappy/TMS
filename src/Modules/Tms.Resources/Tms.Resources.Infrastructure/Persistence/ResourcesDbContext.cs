using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Resources.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options, IPublisher publisher) : DbContext(options)
{
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

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.DispatchDomainEventsAsync(this, publisher, cancellationToken);
        return result;
    }
}
