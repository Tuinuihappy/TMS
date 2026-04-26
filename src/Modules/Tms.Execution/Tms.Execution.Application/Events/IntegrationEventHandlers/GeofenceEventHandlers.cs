using Tms.Execution.Application;
using MediatR;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Events.IntegrationEventHandlers;

public sealed class VehicleEnteredZoneShipmentHandler(
    IShipmentRepository repo,
    IExecutionOutboxWriter outbox)
    : INotificationHandler<VehicleEnteredZoneIntegrationEvent>
{
    public async Task Handle(VehicleEnteredZoneIntegrationEvent ev, CancellationToken ct)
    {
        bool isPickupZone = string.Equals(
            ev.ZoneStopType, "Pickup", StringComparison.OrdinalIgnoreCase);

        if (isPickupZone)
            await HandlePickupZoneAsync(ev, ct);
        else
            await HandleDropoffZoneAsync(ev, ct);
    }

    private async Task HandlePickupZoneAsync(VehicleEnteredZoneIntegrationEvent ev, CancellationToken ct)
    {
        var shipments = await repo.GetActiveByVehiclePickupLocationAsync(
            ev.VehicleId, ev.LocationId, ev.TenantId, ct);

        foreach (var shipment in shipments)
        {
            if (shipment.Status != Domain.Enums.ShipmentStatus.Pending) continue;
            shipment.PickUp();
            outbox.Stage(new ShipmentPickedUpIntegrationEvent(
                shipment.Id, shipment.TripId, shipment.OrderId));
            await repo.UpdateAsync(shipment, ct);
        }
    }

    private async Task HandleDropoffZoneAsync(VehicleEnteredZoneIntegrationEvent ev, CancellationToken ct)
    {
        var shipments = await repo.GetActiveByVehicleDropoffLocationAsync(
            ev.VehicleId, ev.LocationId, ev.TenantId, ct);

        foreach (var shipment in shipments)
        {
            if (shipment.Status is not (
                Domain.Enums.ShipmentStatus.PickedUp or
                Domain.Enums.ShipmentStatus.InTransit)) continue;

            shipment.Arrive();
            outbox.Stage(new ShipmentArrivedAtDropoffIntegrationEvent(
                shipment.Id, shipment.TripId,
                shipment.DropoffStopId, shipment.OrderId));
            await repo.UpdateAsync(shipment, ct);
        }
    }
}
