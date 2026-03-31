using MediatR;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.Execution.Application.Features.GetShipments;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Features.EventHandlers;

/// <summary>
/// When a Trip is dispatched, auto-create Shipment records for each Stop.
/// This is the key cross-module integration: Planning → Execution.
/// </summary>
public sealed class TripDispatchedShipmentCreator(IShipmentRepository repo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent notification, CancellationToken ct)
    {
        foreach (var stop in notification.Stops.OrderBy(s => s.Sequence))
        {
            var shipmentNumber = await repo.GenerateShipmentNumberAsync(ct);

            var shipment = Shipment.Create(
                shipmentNumber,
                notification.TripId,
                stop.OrderId,
                stop.StopId,
                notification.TenantId,
                stop.AddressName,
                stop.AddressStreet,
                stop.AddressProvince,
                stop.Latitude,
                stop.Longitude);

            // Add a default item for the shipment (based on stop/order)
            var item = ShipmentItem.Create(
                shipment.Id,
                $"Order {stop.OrderId} - Stop #{stop.Sequence}",
                expectedQty: 1,
                sku: null);
            shipment.AddItem(item);

            await repo.AddAsync(shipment, ct);
        }
    }
}

/// <summary>
/// When a Trip is cancelled, auto-cancel all its pending Shipments.
/// </summary>
public sealed class TripCancelledShipmentHandler(IShipmentRepository repo)
    : INotificationHandler<TripCancelledIntegrationEvent>
{
    public async Task Handle(TripCancelledIntegrationEvent notification, CancellationToken ct)
    {
        var shipments = await repo.GetByTripIdAsync(notification.TripId, ct);

        foreach (var shipment in shipments)
        {
            // Only cancel pending shipments (not already delivered, etc.)
            if (shipment.Status == Execution.Domain.Enums.ShipmentStatus.Pending)
            {
                shipment.RecordException(
                    $"Trip cancelled: {notification.Reason}",
                    "TRIP_CANCELLED");
                await repo.UpdateAsync(shipment, ct);
            }
        }
    }
}
