using MediatR;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Events.IntegrationEventHandlers;

public sealed class TripDispatchedShipmentCreator(IShipmentRepository repo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent notification, CancellationToken ct)
    {
        var stopsByOrder = notification.Stops.GroupBy(s => s.OrderId);

        foreach (var group in stopsByOrder)
        {
            var orderId = group.Key;
            var stops = group.ToList();

            var dropoffStop = stops.FirstOrDefault(s =>
                string.Equals(s.StopType, "Dropoff", StringComparison.OrdinalIgnoreCase));

            if (dropoffStop is null) continue;

            var existing = await repo.GetByTripAndOrderAsync(notification.TripId, orderId, ct);
            if (existing is not null) continue;

            var shipmentNumber = await repo.GenerateShipmentNumberAsync(ct);

            var shipment = Shipment.Create(
                shipmentNumber,
                notification.TripId,
                orderId,
                dropoffStop.StopId,
                notification.TenantId,
                dropoffStop.AddressName,
                dropoffStop.AddressStreet,
                dropoffStop.AddressProvince,
                dropoffStop.Latitude,
                dropoffStop.Longitude);

            var item = ShipmentItem.Create(
                shipment.Id,
                $"Order {orderId}",
                expectedQty: 1,
                sku: null);
            shipment.AddItem(item);

            await repo.AddAsync(shipment, ct);
        }
    }
}

public sealed class TripCancelledShipmentHandler(IShipmentRepository repo)
    : INotificationHandler<TripCancelledIntegrationEvent>
{
    public async Task Handle(TripCancelledIntegrationEvent notification, CancellationToken ct)
    {
        var shipments = await repo.GetByTripIdAsync(notification.TripId, ct);

        foreach (var shipment in shipments)
        {
            if (shipment.Status == Domain.Enums.ShipmentStatus.Pending)
            {
                shipment.RecordException(
                    $"Trip cancelled: {notification.Reason}",
                    "TRIP_CANCELLED");
                await repo.UpdateAsync(shipment, ct);
            }
        }
    }
}
