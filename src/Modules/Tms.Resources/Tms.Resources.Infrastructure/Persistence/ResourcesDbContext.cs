using Microsoft.EntityFrameworkCore;
using Tms.Resources.Domain.Entities;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : DbContext(options)
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
        base.OnModelCreating(modelBuilder);
    }
}
