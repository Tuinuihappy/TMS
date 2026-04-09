using MediatR;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Features.EventHandlers;

/// <summary>
/// เมื่อรถเข้า GeoZone — แยก logic ตาม ZoneStopType:
///   - Pickup zone  → match PickupLocationId → auto PickUp()
///   - Dropoff zone → match DestinationLocationId → auto Arrive()
/// ทั้งสองกรณี stage integration events ผ่าน IOutboxWriter (transactional)
/// </summary>
public sealed class VehicleEnteredZoneShipmentHandler(
    IShipmentRepository repo,
    IOutboxWriter outbox)
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

    // ── Pickup Zone ─────────────────────────────────────────────────────
    private async Task HandlePickupZoneAsync(
        VehicleEnteredZoneIntegrationEvent ev, CancellationToken ct)
    {
        // Match via PickupLocationId (which now exists on Shipment)
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

    // ── Dropoff Zone ─────────────────────────────────────────────────────
    private async Task HandleDropoffZoneAsync(
        VehicleEnteredZoneIntegrationEvent ev, CancellationToken ct)
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
