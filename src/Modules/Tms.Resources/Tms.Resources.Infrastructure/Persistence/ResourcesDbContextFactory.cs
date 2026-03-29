using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContextFactory : IDesignTimeDbContextFactory<ResourcesDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5433;Database=tms_db;Username=tms_user;Password=tms_pass;";

    public ResourcesDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<ResourcesDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "res"));
        return new ResourcesDbContext(optionsBuilder.Options);
    }
}
