using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tms.Documents.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core CLI (dotnet ef migrations) — not used at runtime.
/// Set env: ConnectionStrings__TmsDb or pass --connection on CLI.
/// </summary>
public sealed class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    private const string DefaultConn =
        "Host=localhost;Port=5434;Database=tms_dev;Username=tms_admin;Password=tms_dev_password;";

    public DocumentsDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__TmsDb") ?? DefaultConn;
        var optionsBuilder = new DbContextOptionsBuilder<DocumentsDbContext>();
        optionsBuilder.UseNpgsql(
            conn,
            x => x.MigrationsHistoryTable("__EFMigrationsHistory", "doc"));
        return new DocumentsDbContext(
            optionsBuilder.Options,
            Tms.SharedKernel.Application.NullPublisher.Instance);
    }
}

