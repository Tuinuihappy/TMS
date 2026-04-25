using MediatR;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

public sealed class RoutePlanLockedCreateTripHandler(ITripRepository tripRepo)
    : INotificationHandler<RoutePlanLockedIntegrationEvent>
{
    public async Task Handle(RoutePlanLockedIntegrationEvent notification, CancellationToken ct)
    {
        var tripNumber = await tripRepo.GenerateTripNumberAsync(ct);

        var trip = Trip.Create(
            tripNumber,
            notification.PlannedDate.ToDateTime(TimeOnly.MinValue),
            notification.TenantId,
            totalWeight: 0,
            totalVolumeCBM: 0);

        foreach (var stop in notification.Stops.OrderBy(s => s.Sequence))
        {
            var stopType = Enum.TryParse<StopType>(stop.StopType, ignoreCase: true, out var parsed)
                ? parsed
                : StopType.Dropoff;

            trip.AddStop(
                stop.Sequence,
                stop.OrderId,
                stopType,
                addressName: null,
                addressStreet: null,
                addressProvince: null,
                lat: stop.Latitude,
                lng: stop.Longitude,
                windowFrom: null,
                windowTo: stop.EstimatedArrivalTime);
        }

        await tripRepo.AddAsync(trip, ct);
    }
}
