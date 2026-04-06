using Tms.Integration.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Integration.Domain.Aggregates;

/// <summary>
/// Aggregate Root สำหรับ ERP Export Job — ส่งบิล AR หรือต้นทุน AP ไป ERP.
/// </summary>
public sealed class ErpExportJob : AggregateRoot
{
    public string ErpProviderCode { get; private set; } = default!;
    public ErpExportType ExportType { get; private set; }
    public ExportJobStatus Status { get; private set; }
    public DateOnly PeriodFrom { get; private set; }
    public DateOnly PeriodTo { get; private set; }
    public int RecordsTotal { get; private set; }
    public int RecordsSucceeded { get; private set; }
    public int RecordsFailed { get; private set; }
    public string? ErrorSummary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid TenantId { get; private set; }

    private ErpExportJob() { }

    public static ErpExportJob Create(
        string erpProviderCode,
        ErpExportType exportType,
        DateOnly periodFrom,
        DateOnly periodTo,
        Guid? createdBy,
        Guid tenantId)
    {
        return new ErpExportJob
        {
            Id = Guid.NewGuid(),
            ErpProviderCode = erpProviderCode,
            ExportType = exportType,
            Status = ExportJobStatus.Queued,
            PeriodFrom = periodFrom,
            PeriodTo = periodTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            TenantId = tenantId
        };
    }

    public void Start(int recordsTotal)
    {
        Status = ExportJobStatus.Running;
        RecordsTotal = recordsTotal;
    }

    public void RecordSuccess() => RecordsSucceeded++;

    public void RecordFailure() => RecordsFailed++;

    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
        Status = RecordsFailed == 0 ? ExportJobStatus.Completed : ExportJobStatus.CompletedWithErrors;
    }

    public void Fail(string reason)
    {
        Status = ExportJobStatus.Failed;
        ErrorSummary = reason;
        CompletedAt = DateTime.UtcNow;
    }
}
