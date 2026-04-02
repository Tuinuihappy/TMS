using MediatR;
using Tms.Planning.Application.Features;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features.EventHandlers;

/// <summary>
/// เมื่อ RoutePlan ถูก Lock → สร้าง Trip อัตโนมัติใน Planning domain
/// (Trip ถูกสร้างโดยรับ RoutePlanLockedIntegrationEvent)
/// </summary>
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
            trip.AddStop(
                stop.Sequence,
                stop.OrderId,
                StopType.Delivery,
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
