using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Execution.Infrastructure.Persistence;

public sealed class ExecutionDbContextFactory : IDesignTimeDbContextFactory<ExecutionDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5434;Database=tms_dev;Username=tms_admin;Password=tms_dev_password;";

    public ExecutionDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<ExecutionDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "exe"));
        return new ExecutionDbContext(optionsBuilder.Options);
    }
}
