using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Infrastructure;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Shared DbContext for cross-cutting concerns: idempotency records.
/// Uses its own "idm" schema and its own connection — separate from module DbContexts.
/// </summary>
public sealed class IdempotencyDbContext(DbContextOptions<IdempotencyDbContext> options)
    : DbContext(options)
{
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("idm");

        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("IdempotencyRecords");
            e.HasKey(x => x.IdempotencyKey);
            e.Property(x => x.IdempotencyKey).HasMaxLength(200);
            e.Property(x => x.CommandType).HasMaxLength(500).IsRequired();
            e.Property(x => x.ResultJson).HasColumnType("text");
            e.HasIndex(x => new { x.TenantId, x.ProcessedAt });
        });

        base.OnModelCreating(modelBuilder);
    }
}
