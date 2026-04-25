using MediatR;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

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
            if (stop.Status == StopStatus.Pending) stop.Arrive();
            if (stop.Status == StopStatus.Arrived) stop.Complete();
        }

        await repo.UpdateAsync(trip, ct);
    }
}

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

        if (stop.Status == StopStatus.Pending) stop.Arrive();
        if (stop.Status == StopStatus.Arrived) stop.Complete();

        if (trip.TryAutoComplete())
        {
            outbox.Stage(new TripCompletedIntegrationEvent(
                trip.Id, trip.TripNumber, trip.VehicleId, trip.DriverId));
        }

        await repo.UpdateAsync(trip, ct);
    }
}
