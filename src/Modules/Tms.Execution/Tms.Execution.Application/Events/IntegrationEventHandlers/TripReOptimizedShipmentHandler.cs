using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Execution.Domain.Enums;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Events.IntegrationEventHandlers;

public sealed class TripReOptimizedShipmentHandler(
    IShipmentRepository repo,
    ILogger<TripReOptimizedShipmentHandler> logger)
    : INotificationHandler<TripReOptimizedIntegrationEvent>
{
    public async Task Handle(TripReOptimizedIntegrationEvent notification, CancellationToken ct)
    {
        var shipments = await repo.GetByTripIdAsync(notification.TripId, ct);

        var pending = shipments.Where(s => s.Status == ShipmentStatus.Pending).ToList();
        var inProgress = shipments
            .Where(s => s.Status is ShipmentStatus.PickedUp or ShipmentStatus.InTransit)
            .ToList();

        if (pending.Count > 0)
            logger.LogInformation(
                "Trip {TripNumber} re-optimized — {Count} pending shipment(s) will follow new route order.",
                notification.TripNumber, pending.Count);

        if (inProgress.Count > 0)
            logger.LogWarning(
                "Trip {TripNumber} re-optimized while {Count} shipment(s) are in-progress: {ShipmentNumbers}.",
                notification.TripNumber, inProgress.Count,
                string.Join(", ", inProgress.Select(s => s.ShipmentNumber)));

        foreach (var shipment in inProgress)
        {
            shipment.RecordException(
                $"Route re-optimized for trip {notification.TripNumber}. Please check updated stop order.",
                "ROUTE_REOPTIMIZED");
            await repo.UpdateAsync(shipment, ct);
        }
    }
}
