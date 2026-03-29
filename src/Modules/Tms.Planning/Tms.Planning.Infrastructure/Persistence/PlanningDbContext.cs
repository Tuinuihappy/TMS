using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Stop> Stops => Set<Stop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pln");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
