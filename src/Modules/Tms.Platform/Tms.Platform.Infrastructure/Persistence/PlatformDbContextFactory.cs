using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5434;Database=tms_dev;Username=tms_admin;Password=tms_dev_password;";

    public PlatformDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        optionsBuilder.UseNpgsql(conn, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "plf"));
        return new PlatformDbContext(optionsBuilder.Options, Tms.SharedKernel.Application.NullPublisher.Instance);
    }
}
