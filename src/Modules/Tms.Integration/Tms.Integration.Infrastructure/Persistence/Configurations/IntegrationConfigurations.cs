using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;

namespace Tms.Integration.Infrastructure.Persistence.Configurations;

public sealed class OmsOrderSyncConfiguration : IEntityTypeConfiguration<OmsOrderSync>
{
    public void Configure(EntityTypeBuilder<OmsOrderSync> b)
    {
        b.ToTable("OmsOrderSyncs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalOrderRef).HasMaxLength(200).IsRequired();
        b.Property(x => x.OmsProviderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.ExternalOrderRef, x.OmsProviderCode }).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.NextRetryAt);
    }
}

public sealed class OmsFieldMappingConfiguration : IEntityTypeConfiguration<OmsFieldMapping>
{
    public void Configure(EntityTypeBuilder<OmsFieldMapping> b)
    {
        b.ToTable("OmsFieldMappings");
        b.HasKey(x => x.Id);
        b.Property(x => x.OmsProviderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.OmsField).HasMaxLength(200).IsRequired();
        b.Property(x => x.TmsField).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.OmsProviderCode);
    }
}

public sealed class OmsOutboxEventConfiguration : IEntityTypeConfiguration<OmsOutboxEvent>
{
    public void Configure(EntityTypeBuilder<OmsOutboxEvent> b)
    {
        b.ToTable("OmsOutboxEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.OmsProviderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => x.Status);
    }
}

public sealed class AmrHandoffRecordConfiguration : IEntityTypeConfiguration<AmrHandoffRecord>
{
    public void Configure(EntityTypeBuilder<AmrHandoffRecord> b)
    {
        b.ToTable("AmrHandoffRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.AmrJobId).HasMaxLength(200).IsRequired();
        b.Property(x => x.AmrProviderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.DockCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.AmrJobId, x.AmrProviderCode }).IsUnique();
        b.HasIndex(x => x.ShipmentId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.TenantId);
    }
}

public sealed class DockStationConfiguration : IEntityTypeConfiguration<DockStation>
{
    public void Configure(EntityTypeBuilder<DockStation> b)
    {
        b.ToTable("DockStations");
        b.HasKey(x => x.Id);
        b.Property(x => x.DockCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.WarehouseCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.DockCode).IsUnique();
        b.HasIndex(x => x.WarehouseCode);
    }
}

public sealed class ErpExportJobConfiguration : IEntityTypeConfiguration<ErpExportJob>
{
    public void Configure(EntityTypeBuilder<ErpExportJob> b)
    {
        b.ToTable("ErpExportJobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.ErpProviderCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.ExportType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => x.ExportType);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.PeriodFrom, x.PeriodTo });
    }
}

public sealed class ErpExportRecordConfiguration : IEntityTypeConfiguration<ErpExportRecord>
{
    public void Configure(EntityTypeBuilder<ErpExportRecord> b)
    {
        b.ToTable("ErpExportRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
        b.Property(x => x.ErpDocumentRef).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.JobId);
        b.HasIndex(x => x.SourceId);
        b.HasIndex(x => x.Status);
    }
}

public sealed class ErpReconciliationConfiguration : IEntityTypeConfiguration<ErpReconciliation>
{
    public void Configure(EntityTypeBuilder<ErpReconciliation> b)
    {
        b.ToTable("ErpReconciliations");
        b.HasKey(x => x.Id);
        b.Property(x => x.ErpPaymentRef).HasMaxLength(200).IsRequired();
        b.Property(x => x.InvoiceNumber).HasMaxLength(100).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.AmountPaid).HasPrecision(15, 2);
        b.HasIndex(x => x.ErpPaymentRef).IsUnique();
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.InvoiceId);
    }
}
