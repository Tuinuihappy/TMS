using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Orders.Infrastructure.Persistence;

/// <summary>Used by EF Core CLI (dotnet ef migrations) — not used at runtime.
/// Set env: ConnectionStrings__TmsDb or pass --connection on CLI.
/// </summary>
public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5433;Database=tms_db;Username=tms_user;Password=tms_pass;";

    public OrdersDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseNpgsql(
            conn,
            x => x.MigrationsHistoryTable("__EFMigrationsHistory", "ord"));
        return new OrdersDbContext(optionsBuilder.Options);
    }
}
