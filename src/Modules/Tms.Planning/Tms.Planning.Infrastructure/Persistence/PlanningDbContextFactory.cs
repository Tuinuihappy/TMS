using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Planning.Infrastructure.Persistence;

public sealed class PlanningDbContextFactory : IDesignTimeDbContextFactory<PlanningDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5434;Database=tms_dev;Username=tms_admin;Password=tms_dev_password;";

    public PlanningDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<PlanningDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "pln"));
        return new PlanningDbContext(optionsBuilder.Options, Tms.SharedKernel.Application.NullPublisher.Instance);
    }
}
