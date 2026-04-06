using Tms.Integration.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Integration.Domain.Aggregates;

/// <summary>
/// Aggregate Root สำหรับ Inbound Order Sync จาก OMS.
/// ติดตามสถานะ Webhook ที่รับเข้ามาจนกว่าจะสร้าง TMS Order สำเร็จ.
/// </summary>
public sealed class OmsOrderSync : AggregateRoot
{
    public string ExternalOrderRef { get; private set; } = default!;
    public string OmsProviderCode { get; private set; } = default!;
    public Guid? TmsOrderId { get; private set; }
    public SyncDirection Direction { get; private set; }
    public SyncStatus Status { get; private set; }
    public string RawPayload { get; private set; } = default!;
    public string? MappedPayload { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public Guid TenantId { get; private set; }

    private OmsOrderSync() { }

    public static OmsOrderSync CreateInbound(
        string externalOrderRef,
        string omsProviderCode,
        string rawPayload,
        Guid tenantId)
    {
        return new OmsOrderSync
        {
            Id = Guid.NewGuid(),
            ExternalOrderRef = externalOrderRef,
            OmsProviderCode = omsProviderCode,
            Direction = SyncDirection.Inbound,
            Status = SyncStatus.Pending,
            RawPayload = rawPayload,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    public void MarkProcessing(string mappedPayload)
    {
        Status = SyncStatus.Processing;
        MappedPayload = mappedPayload;
    }

    public void MarkSucceeded(Guid tmsOrderId)
    {
        Status = SyncStatus.Succeeded;
        TmsOrderId = tmsOrderId;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string reason)
    {
        RetryCount++;
        ErrorMessage = reason;

        if (RetryCount >= 5)
        {
            Status = SyncStatus.DeadLetter;
            NextRetryAt = null;
        }
        else
        {
            Status = SyncStatus.Failed;
            // Exponential backoff: 1min, 2min, 4min, 8min, 16min
            var delayMinutes = Math.Pow(2, RetryCount - 1);
            NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        }
    }

    public void ResetForRetry()
    {
        if (Status != SyncStatus.DeadLetter)
            throw new InvalidOperationException("Only DeadLetter syncs can be manually reset.");
        RetryCount = 0;
        Status = SyncStatus.Pending;
        NextRetryAt = null;
        ErrorMessage = null;
    }
}
