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

/// <summary>
/// Fired when a Dispatched/InProgress trip's remaining stops are re-optimized mid-execution.
/// Execution module can update driver app display in real-time.
/// </summary>
public sealed record TripReOptimizedIntegrationEvent(
    Guid TripId,
    string TripNumber,
    List<TripStopSnapshot> ReorderedStops) : IntegrationEvent;


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

/// <summary>
/// Execution → Planning: Shipment ถูก PickUp แล้ว
/// Planning จะ mark Pickup Stops ทุกอันของ Order นั้นใน Trip เป็น Completed
/// </summary>
public sealed record ShipmentPickedUpIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid OrderId) : IntegrationEvent;

/// <summary>
/// Execution → Planning: Shipment ถึง Dropoff Stop แล้ว (Arrived)
/// Planning จะ mark Dropoff Stop เป็น Arrived
/// </summary>
public sealed record ShipmentArrivedAtDropoffIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid DropoffStopId,
    Guid OrderId) : IntegrationEvent;

/// <summary>
/// Execution → Planning: Shipment Delivered สำเร็จ
/// Planning จะ mark Dropoff Stop เป็น Completed
/// </summary>
public sealed record ShipmentDeliveredStopIntegrationEvent(
    Guid ShipmentId,
    Guid TripId,
    Guid DropoffStopId,
    Guid OrderId) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// TRACKING MODULE — Geofencing & ETA Events (Phase 2)
// ══════════════════════════════════════════════════════════════════════

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

// ══════════════════════════════════════════════════════════════════════
// PLANNING MODULE — Route Plan Events (Phase 2)
// ══════════════════════════════════════════════════════════════════════

public sealed record RoutePlanLockedIntegrationEvent(
    Guid RoutePlanId,
    Guid? VehicleTypeId,
    DateOnly PlannedDate,
    Guid TenantId,
    List<RoutePlanStopSnapshot> Stops) : IntegrationEvent;

public sealed record RoutePlanStopSnapshot(
    int Sequence,
    Guid OrderId,
    /// <summary>"Pickup" | "Dropoff"</summary>
    string StopType,
    double Latitude,
    double Longitude,
    DateTime? EstimatedArrivalTime);

// ══════════════════════════════════════════════════════════════════════
// INTEGRATION MODULE — OMS / AMR / ERP Events (Phase 4)
// ══════════════════════════════════════════════════════════════════════

public sealed record OrderSyncedFromOmsIntegrationEvent(
    Guid SyncId,
    string ExternalOrderRef,
    string OmsProviderCode,
    Guid TmsOrderId,
    string OrderNumber) : IntegrationEvent;

public sealed record DockReadyIntegrationEvent(
    string AmrJobId,
    string AmrProviderCode,
    string DockCode,
    Guid ShipmentId,
    int ItemsReady) : IntegrationEvent;

public sealed record InventoryHandoffConfirmedIntegrationEvent(
    Guid HandoffId,
    string AmrJobId,
    Guid ShipmentId,
    string DockCode,
    int ItemsExpected,
    int ItemsActual,
    string Status) : IntegrationEvent;

public sealed record ErpInvoiceExportedIntegrationEvent(
    Guid ExportRecordId,
    Guid InvoiceId,
    string InvoiceNumber,
    string ErpDocumentRef,
    string ErpProvider) : IntegrationEvent;

public sealed record PaymentReconciliationMatchedIntegrationEvent(
    Guid ReconciliationId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal AmountPaid,
    DateOnly PaidAt) : IntegrationEvent;

// ══════════════════════════════════════════════════════════════════════
// DOCUMENTS MODULE (Phase 4)
// ══════════════════════════════════════════════════════════════════════

public sealed record DocumentUploadedIntegrationEvent(
    Guid DocumentId,
    string Category,
    Guid OwnerId,
    string OwnerType,
    string FileName,
    string ContentType,
    Guid TenantId) : IntegrationEvent;
