using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pln");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);

        modelBuilder.Entity<Trip>(builder =>
        {
            builder.ToTable("Trips");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TripNumber).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.TripNumber).IsUnique();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        base.OnModelCreating(modelBuilder);
    }
}
