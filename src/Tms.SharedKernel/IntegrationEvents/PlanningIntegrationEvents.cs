using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record TripDispatchedIntegrationEvent(
    Guid TripId,
    string TripNumber,
    Guid VehicleId,
    Guid DriverId,
    Guid TenantId,
    List<TripStopSnapshot> Stops) : IntegrationEvent;

public sealed record TripStopSnapshot(
    Guid StopId,
    int Sequence,
    Guid OrderId,
    string StopType,
    string? AddressName,
    string? AddressStreet,
    string? AddressProvince,
    double? Latitude,
    double? Longitude);

public sealed record TripCancelledIntegrationEvent(
    Guid TripId,
    string TripNumber,
    string Reason,
    List<TripStopSnapshot> Stops,
    Guid? VehicleId = null,
    Guid? DriverId = null) : IntegrationEvent;

public sealed record TripCompletedIntegrationEvent(
    Guid TripId,
    string TripNumber,
    Guid? VehicleId,
    Guid? DriverId) : IntegrationEvent;

/// <summary>
/// Fired when a Dispatched/InProgress trip's remaining stops are re-optimized mid-execution.
/// </summary>
public sealed record TripReOptimizedIntegrationEvent(
    Guid TripId,
    string TripNumber,
    List<TripStopSnapshot> ReorderedStops) : IntegrationEvent;

public sealed record RoutePlanLockedIntegrationEvent(
    Guid RoutePlanId,
    Guid? VehicleTypeId,
    DateOnly PlannedDate,
    Guid TenantId,
    List<RoutePlanStopSnapshot> Stops) : IntegrationEvent;

public sealed record RoutePlanStopSnapshot(
    int Sequence,
    Guid OrderId,
    string StopType,
    double Latitude,
    double Longitude,
    DateTime? EstimatedArrivalTime);
