using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Infrastructure.Persistence;

public sealed class ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<PODRecord> PODRecords => Set<PODRecord>();
    public DbSet<PODPhoto> PODPhotos => Set<PODPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("exe");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExecutionDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
