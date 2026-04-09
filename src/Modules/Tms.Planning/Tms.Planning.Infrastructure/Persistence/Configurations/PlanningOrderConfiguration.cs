using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Infrastructure.Persistence.Configurations;

public sealed class PlanningOrderConfiguration : IEntityTypeConfiguration<PlanningOrder>
{
    public void Configure(EntityTypeBuilder<PlanningOrder> builder)
    {
        builder.ToTable("PlanningOrders");
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(o => o.OrderId).IsUnique();
        
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        
        builder.Property(o => o.TotalWeight).HasPrecision(18, 2);
        builder.Property(o => o.TotalVolume).HasPrecision(18, 2);
        
        // Concurrency token สำหรับ State Locking (Idempotency Key / Session Id)
        builder.Property(o => o.CurrentProcessingSessionId).IsConcurrencyToken();
    }
}
