using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Infrastructure.Persistence.Configurations;

internal sealed class PODDocumentConfiguration : IEntityTypeConfiguration<PODDocument>
{
    public void Configure(EntityTypeBuilder<PODDocument> builder)
    {
        builder.ToTable("PODDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ShipmentId).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.DocumentReference).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CapturedAt).IsRequired();
        builder.Property(x => x.GeotagDistanceDifferenceMeters).HasPrecision(10, 2);

        builder.HasMany(x => x.Verifications)
            .WithOne()
            .HasForeignKey(v => v.PODDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ShipmentId);
    }
}

internal sealed class VerificationItemConfiguration : IEntityTypeConfiguration<VerificationItem>
{
    public void Configure(EntityTypeBuilder<VerificationItem> builder)
    {
        builder.ToTable("VerificationItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PODDocumentId).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BlobUrl).HasMaxLength(1000).IsRequired();
    }
}
