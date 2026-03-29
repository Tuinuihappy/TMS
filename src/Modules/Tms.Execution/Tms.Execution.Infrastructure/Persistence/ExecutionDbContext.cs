using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Infrastructure.Persistence;

public sealed class ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("exe");
        modelBuilder.Entity<Shipment>(b =>
        {
            b.ToTable("Shipments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
        base.OnModelCreating(modelBuilder);
    }
}
