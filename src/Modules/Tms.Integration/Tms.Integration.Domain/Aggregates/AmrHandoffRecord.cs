using Tms.Integration.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Integration.Domain.Aggregates;

/// <summary>
/// Aggregate Root สำหรับการรับ-โอนสินค้าจาก AMR ที่ Dock.
/// </summary>
public sealed class AmrHandoffRecord : AggregateRoot
{
    public string AmrJobId { get; private set; } = default!;
    public string AmrProviderCode { get; private set; } = default!;
    public Guid ShipmentId { get; private set; }
    public string DockCode { get; private set; } = default!;
    public HandoffStatus Status { get; private set; }
    public string? RawAmrPayload { get; private set; }
    public int ItemsExpected { get; private set; }
    public int? ItemsActual { get; private set; }
    public string? DriverNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? HandoffConfirmedAt { get; private set; }
    public Guid TenantId { get; private set; }

    private AmrHandoffRecord() { }

    public static AmrHandoffRecord Create(
        string amrJobId,
        string amrProviderCode,
        Guid shipmentId,
        string dockCode,
        int itemsExpected,
        string? rawPayload,
        Guid tenantId)
    {
        return new AmrHandoffRecord
        {
            Id = Guid.NewGuid(),
            AmrJobId = amrJobId,
            AmrProviderCode = amrProviderCode,
            ShipmentId = shipmentId,
            DockCode = dockCode,
            ItemsExpected = itemsExpected,
            RawAmrPayload = rawPayload,
            Status = HandoffStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    public void MarkAmrAtDock() => Status = HandoffStatus.AmrAtDock;

    public void StartTransfer() => Status = HandoffStatus.Transferring;

    public void ConfirmHandoff(int itemsActual, string? driverNote)
    {
        if (Status != HandoffStatus.Transferring)
            throw new InvalidOperationException($"Cannot confirm handoff in status {Status}.");

        if (itemsActual > ItemsExpected)
            throw new InvalidOperationException("Actual items cannot exceed expected items without Admin override.");

        ItemsActual = itemsActual;
        DriverNote = driverNote;
        HandoffConfirmedAt = DateTime.UtcNow;
        Status = itemsActual == ItemsExpected ? HandoffStatus.Confirmed : HandoffStatus.PartialHandoff;
    }

    public void MarkFailed(string reason)
    {
        Status = HandoffStatus.Failed;
        DriverNote = reason;
    }
}
