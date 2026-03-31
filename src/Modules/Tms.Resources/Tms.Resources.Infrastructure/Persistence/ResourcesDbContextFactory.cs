using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Resources.Infrastructure.Persistence;

public sealed class ResourcesDbContextFactory : IDesignTimeDbContextFactory<ResourcesDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5434;Database=tms_dev;Username=tms_admin;Password=tms_dev_password;";

    public ResourcesDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<ResourcesDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "res"));
        return new ResourcesDbContext(optionsBuilder.Options, Tms.SharedKernel.Application.NullPublisher.Instance);
    }
}
