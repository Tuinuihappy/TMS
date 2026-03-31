using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

// ══════════════════════════════════════════════════════════════════════
// PLATFORM MODULE — Master Data & IAM Events
// ══════════════════════════════════════════════════════════════════════

public sealed record CustomerDeactivatedIntegrationEvent(
    Guid CustomerId,
    string CustomerCode) : IntegrationEvent;

public sealed record UserRolesChangedIntegrationEvent(
    Guid UserId,
    string Username,
    List<Guid> RoleIds) : IntegrationEvent;

public sealed record UserDeactivatedIntegrationEvent(
    Guid UserId,
    string Username) : IntegrationEvent;

public sealed record ApiKeyCreatedIntegrationEvent(
    Guid ApiKeyId,
    string Name,
    Guid TenantId) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// RESOURCES MODULE — Vehicle & Driver Events
// ══════════════════════════════════════════════════════════════════════

public sealed record VehicleStatusChangedIntegrationEvent(
    Guid VehicleId,
    string PlateNumber,
    string OldStatus,
    string NewStatus) : IntegrationEvent;

public sealed record DriverStatusChangedIntegrationEvent(
    Guid DriverId,
    string EmployeeCode,
    string OldStatus,
    string NewStatus) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// ORDERS MODULE — Order Events
// ══════════════════════════════════════════════════════════════════════

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason) : IntegrationEvent;

public sealed record OrderAmendedIntegrationEvent(
    Guid OrderId,
    string OrderNumber) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// PLANNING MODULE — Trip & Dispatch Events
// ══════════════════════════════════════════════════════════════════════

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
    Guid? VehicleId = null,
    Guid? DriverId = null) : IntegrationEvent;

public sealed record TripCompletedIntegrationEvent(
    Guid TripId,
    string TripNumber,
    Guid? VehicleId,
    Guid? DriverId) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// EXECUTION MODULE — Shipment Events
// ══════════════════════════════════════════════════════════════════════

public sealed record ShipmentDeliveredIntegrationEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid OrderId,
    Guid TripId,
    DateTime DeliveredAt) : IntegrationEvent;

public sealed record ShipmentExceptionIntegrationEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid OrderId,
    string ReasonCode,
    string Reason) : IntegrationEvent;
