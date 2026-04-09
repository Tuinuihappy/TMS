using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features;

/// <summary>
/// Re-runs PDP optimization on a Trip's remaining PENDING stops only.
/// Used for mid-day re-optimization (e.g., new order added, stop skipped, traffic).
/// The Trip must be Dispatched or InProgress.
/// </summary>
public sealed record ReOptimizeTripCommand(
    Guid TripId,
    double DepotLat = 0,
    double DepotLng = 0,
    decimal MaxCapacityKg = 10_000m,
    decimal MaxCapacityVolumeCBM = 0m,
    DateTime? DepartureTime = null) : ICommand;

public sealed class ReOptimizeTripHandler(
    ITripRepository repo,
    PdpRouteOptimizer optimizer,
    IOutboxWriter outbox)
    : ICommandHandler<ReOptimizeTripCommand>
{
    public async Task Handle(ReOptimizeTripCommand req, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(req.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), req.TripId);

        if (trip.Status is not (TripStatus.Dispatched or TripStatus.InProgress))
            throw new DomainException(
                "Can only re-optimize Dispatched or InProgress trips.",
                "INVALID_TRIP_STATE");

        // Collect pending stops only — Arrived/Completed/Skipped are already done
        var pendingStops = trip.Stops
            .Where(s => s.Status == StopStatus.Pending)
            .ToList();

        if (pendingStops.Count < 2)
            return; // Nothing meaningful to re-optimize

        // Build PDP inputs from pending stops
        // Group by OrderId to form (Pickup, Dropoff) pairs
        var orderIds = pendingStops.Select(s => s.OrderId).Distinct().ToList();
        var orderInputs = new List<PdpOrderInput>();

        foreach (var orderId in orderIds)
        {
            var pickup  = pendingStops.FirstOrDefault(s => s.OrderId == orderId && s.Type == StopType.Pickup);
            var dropoff = pendingStops.FirstOrDefault(s => s.OrderId == orderId && s.Type == StopType.Dropoff);

            if (pickup is null || dropoff is null) continue; // Partial — skip unpaired

            orderInputs.Add(new PdpOrderInput(
                orderId,
                pickup.AddressLatitude  ?? 0, pickup.AddressLongitude  ?? 0,
                dropoff.AddressLatitude ?? 0, dropoff.AddressLongitude ?? 0,
                DropoffWindowFrom: dropoff.WindowFrom,
                DropoffWindowTo:   dropoff.WindowTo,
                PickupWindowFrom:  pickup.WindowFrom,
                PickupWindowTo:    pickup.WindowTo));
        }

        if (orderInputs.Count == 0) return;

        // Re-optimize single route (already assigned to this trip's vehicle)
        var routes = optimizer.Optimize(
            orderInputs,
            maxOrdersPerRoute: orderIds.Count,
            maxCapacityKg: req.MaxCapacityKg,
            maxCapacityVolumeCBM: req.MaxCapacityVolumeCBM,
            depotLat: req.DepotLat,
            depotLng: req.DepotLng,
            departureTime: req.DepartureTime ?? DateTime.UtcNow);

        if (routes.Count == 0) return;

        var optimizedRoute = routes[0];

        // Re-sequence the pending stops to match optimized order
        var sequence = trip.Stops
            .Where(s => s.Status != StopStatus.Pending)
            .Max(s => s.Sequence);

        foreach (var pdpStop in optimizedRoute)
        {
            var stop = pendingStops.FirstOrDefault(s =>
                s.OrderId == pdpStop.OrderId &&
                s.Type.ToString() == pdpStop.StopType);

            if (stop is null) continue;

            stop.UpdateSequence(++sequence);
        }

        // Stage notification that route was re-planned (Execution will reorder display)
        outbox.Stage(new TripReOptimizedIntegrationEvent(
            trip.Id, trip.TripNumber,
            optimizedRoute.Select((s, i) => new TripStopSnapshot(
                Guid.Empty, i + 1, s.OrderId, s.StopType,
                null, null, null, s.Lat, s.Lng)).ToList()));

        await repo.UpdateAsync(trip, ct);
    }
}
