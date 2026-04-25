using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

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

/// <summary>Execution → Planning: Shipment ถูก PickUp → mark Pickup Stops เป็น Completed</summary>
public sealed record ShipmentPickedUpIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid OrderId) : IntegrationEvent;

/// <summary>Execution → Planning: Shipment ถึง Dropoff Stop → mark เป็น Arrived</summary>
public sealed record ShipmentArrivedAtDropoffIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid DropoffStopId,
    Guid OrderId) : IntegrationEvent;

/// <summary>Execution → Planning: Shipment Delivered สำเร็จ → mark Dropoff Stop เป็น Completed</summary>
public sealed record ShipmentDeliveredStopIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid DropoffStopId,
    Guid OrderId) : IntegrationEvent;
