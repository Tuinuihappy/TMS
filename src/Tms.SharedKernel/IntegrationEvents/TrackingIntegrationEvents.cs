using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record VehicleEnteredZoneIntegrationEvent(
    Guid VehicleId,
    Guid ZoneId,
    Guid LocationId,
    DateTime Timestamp,
    Guid TenantId,
    /// <summary>"Pickup" | "Dropoff" | null — ถ้า null ให้ treat เป็น Dropoff (backward compat)</summary>
    string? ZoneStopType = null) : IntegrationEvent;

public sealed record VehicleETAUpdatedIntegrationEvent(
    Guid TripId,
    int StopSequence,
    Guid OrderId,
    DateTime EstimatedArrivalTime) : IntegrationEvent;
