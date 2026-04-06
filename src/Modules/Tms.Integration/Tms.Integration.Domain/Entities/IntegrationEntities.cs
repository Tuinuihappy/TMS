using Tms.Integration.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Integration.Domain.Entities;

public sealed class OmsFieldMapping : BaseEntity
{
    public string OmsProviderCode { get; set; } = default!;
    public string OmsField { get; set; } = default!;
    public string TmsField { get; set; } = default!;
    public string? TransformExpression { get; set; }
    public bool IsRequired { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class OmsOutboxEvent : BaseEntity
{
    public string IdempotencyKey { get; set; } = default!;
    public string OmsProviderCode { get; set; } = default!;
    public Guid TmsOrderId { get; set; }
    public string ExternalOrderRef { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public Guid TenantId { get; set; }

    public static OmsOutboxEvent Create(
        string omsProviderCode,
        Guid tmsOrderId,
        string externalOrderRef,
        string eventType,
        string payload,
        Guid tenantId)
    {
        return new OmsOutboxEvent
        {
            IdempotencyKey = $"{tmsOrderId}:{eventType}",
            OmsProviderCode = omsProviderCode,
            TmsOrderId = tmsOrderId,
            ExternalOrderRef = externalOrderRef,
            EventType = eventType,
            Payload = payload,
            TenantId = tenantId
        };
    }
}

public sealed class DockStation : BaseEntity
{
    public string DockCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string WarehouseCode { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DockStatus Status { get; set; } = DockStatus.Available;
    public Guid? AssignedVehicleId { get; set; }
    public Guid TenantId { get; set; }
}

public sealed class ErpExportRecord : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid SourceId { get; set; }
    public string SourceType { get; set; } = default!;  // "Invoice" | "Cost"
    public string Payload { get; set; } = default!;
    public RecordStatus Status { get; set; } = RecordStatus.Pending;
    public string? ErpDocumentRef { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}

public sealed class ErpReconciliation : BaseEntity
{
    public string ErpPaymentRef { get; set; } = default!;
    public Guid? InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public ReconciliationStatus Status { get; set; } = ReconciliationStatus.Pending;
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "THB";
    public DateOnly PaidAt { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
