using MediatR;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features.EventHandlers;

/// <summary>
/// เมื่อ Shipment ถูก PickUp → mark Pickup Stops ทุกอันของ Order นั้นใน Trip เป็น Arrived + Completed
/// Rule: 1 Order อาจมีหลาย Pickup Stops (multi-warehouse) — mark ทั้งหมด
/// </summary>
public sealed class ShipmentPickedUpStopHandler(ITripRepository repo)
    : INotificationHandler<ShipmentPickedUpIntegrationEvent>
{
    public async Task Handle(ShipmentPickedUpIntegrationEvent ev, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(ev.TripId, ct);
        if (trip is null) return;

        var pickupStops = trip.Stops
            .Where(s => s.OrderId == ev.OrderId && s.Type == StopType.Pickup)
            .ToList();

        if (pickupStops.Count == 0) return;

        foreach (var stop in pickupStops)
        {
            if (stop.Status == StopStatus.Pending)  stop.Arrive();
            if (stop.Status == StopStatus.Arrived)  stop.Complete();
        }

        await repo.UpdateAsync(trip, ct);
    }
}

/// <summary>
/// เมื่อ Shipment Arrived ที่ Dropoff Stop → mark Dropoff Stop เป็น Arrived
/// </summary>
public sealed class ShipmentArrivedAtDropoffStopHandler(ITripRepository repo)
    : INotificationHandler<ShipmentArrivedAtDropoffIntegrationEvent>
{
    public async Task Handle(ShipmentArrivedAtDropoffIntegrationEvent ev, CancellationToken ct)
    {
        var trip = await repo.GetByStopIdAsync(ev.DropoffStopId, ct);
        if (trip is null) return;

        var stop = trip.Stops.FirstOrDefault(s => s.Id == ev.DropoffStopId);
        if (stop is null || stop.Status != StopStatus.Pending) return;

        stop.Arrive();
        await repo.UpdateAsync(trip, ct);
    }
}

/// <summary>
/// เมื่อ Shipment Delivered → mark Dropoff Stop เป็น Completed
/// แล้ว TryAutoComplete Trip — ถ้าทุก Dropoff เสร็บ Trip จะ Complete อัตโนมัติ
/// </summary>
public sealed class ShipmentDeliveredStopHandler(
    ITripRepository repo,
    IOutboxWriter outbox)
    : INotificationHandler<ShipmentDeliveredStopIntegrationEvent>
{
    public async Task Handle(ShipmentDeliveredStopIntegrationEvent ev, CancellationToken ct)
    {
        var trip = await repo.GetByStopIdAsync(ev.DropoffStopId, ct);
        if (trip is null) return;

        var stop = trip.Stops.FirstOrDefault(s => s.Id == ev.DropoffStopId);
        if (stop is null) return;

        // idempotent: Arrived → Complete
        if (stop.Status == StopStatus.Pending)  stop.Arrive();
        if (stop.Status == StopStatus.Arrived)  stop.Complete();

        // Auto-complete Trip if all Dropoffs done
        if (trip.TryAutoComplete())
        {
            outbox.Stage(new TripCompletedIntegrationEvent(
                trip.Id, trip.TripNumber, trip.VehicleId, trip.DriverId));
        }

        await repo.UpdateAsync(trip, ct);
    }
}

