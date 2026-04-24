using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Execution.Domain.Enums;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Features.EventHandlers;

/// <summary>
/// เมื่อ Trip ถูก Re-optimize กลางทาง → ตรวจสอบ Shipments ที่ยังไม่เสร็จ
/// Pending shipments: ยังไม่ถูก pickup — รอ sequence ใหม่จาก Planning ได้เลย
/// InProgress shipments: อยู่ระหว่างการขนส่ง — log warning ให้ Dispatcher รับรู้
/// </summary>
public sealed class TripReOptimizedShipmentHandler(
    IShipmentRepository repo,
    ILogger<TripReOptimizedShipmentHandler> logger)
    : INotificationHandler<TripReOptimizedIntegrationEvent>
{
    public async Task Handle(TripReOptimizedIntegrationEvent notification, CancellationToken ct)
    {
        var shipments = await repo.GetByTripIdAsync(notification.TripId, ct);

        var pending = shipments
            .Where(s => s.Status == ShipmentStatus.Pending)
            .ToList();

        var inProgress = shipments
            .Where(s => s.Status is ShipmentStatus.PickedUp or ShipmentStatus.InTransit)
            .ToList();

        if (pending.Count > 0)
            logger.LogInformation(
                "Trip {TripNumber} re-optimized — {Count} pending shipment(s) will follow new route order.",
                notification.TripNumber, pending.Count);

        if (inProgress.Count > 0)
            logger.LogWarning(
                "Trip {TripNumber} re-optimized while {Count} shipment(s) are in-progress: {ShipmentNumbers}. " +
                "Driver should check updated route on app.",
                notification.TripNumber, inProgress.Count,
                string.Join(", ", inProgress.Select(s => s.ShipmentNumber)));

        // New stop sequence is already updated in Planning module (Stop.Sequence).
        // Driver app queries Planning for the current stop order — no structural change needed here.
        // If any pending shipments exist, record exception so dispatcher is aware of re-optimization.
        foreach (var shipment in inProgress)
        {
            shipment.RecordException(
                $"Route re-optimized for trip {notification.TripNumber}. Please check updated stop order.",
                "ROUTE_REOPTIMIZED");
            await repo.UpdateAsync(shipment, ct);
        }
    }
}
