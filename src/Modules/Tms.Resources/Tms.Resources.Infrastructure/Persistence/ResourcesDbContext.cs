using Microsoft.EntityFrameworkCore;
using Tms.Resources.Domain.Entities;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : DbContext(options)
{
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("res");

        modelBuilder.Entity<Vehicle>(b =>
        {
            b.ToTable("Vehicles");
            b.HasKey(x => x.Id);
            b.Property(x => x.LicensePlate).IsRequired().HasMaxLength(20);
            b.HasIndex(x => x.LicensePlate).IsUnique();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(x => x.PayloadKg).HasPrecision(10, 2);
            b.Property(x => x.VolumeCbm).HasPrecision(10, 3);
        });

        modelBuilder.Entity<Driver>(b =>
        {
            b.ToTable("Drivers");
            b.HasKey(x => x.Id);
            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(50);
            b.HasIndex(x => x.LicenseNumber).IsUnique();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        base.OnModelCreating(modelBuilder);
    }
}
