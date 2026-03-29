using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5433;Database=tms_db;Username=tms_user;Password=tms_pass;";

    public PlanningDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<PlanningDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "pln"));
        return new PlanningDbContext(optionsBuilder.Options);
    }
}
