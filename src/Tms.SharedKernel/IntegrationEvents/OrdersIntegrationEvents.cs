using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

/// <summary>
/// Snapshot ของ 1 OrderStop ที่ส่งออกใน integration events
/// ใช้ร่วมกันระหว่าง OrderConfirmedIntegrationEvent และ OrderAmendedIntegrationEvent
/// </summary>
public sealed record OrderStopSnapshot(
    Guid StopId,
    int Sequence,
    double PickupLatitude,
    double PickupLongitude,
    string PickupProvince,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffProvince,
    decimal WeightKg,
    decimal VolumeCBM,
    DateTime? ReadyTime,
    DateTime? DueTime,
    DateTime? PickupWindowFrom,
    DateTime? PickupWindowTo,
    DateTime? DropoffWindowFrom,
    DateTime? DropoffWindowTo);

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid TenantId,
    string Priority,
    decimal TotalWeight,
    decimal TotalVolumeCBM,
    int ItemCount,
    bool HasDangerousGoods,
    List<OrderStopSnapshot> Stops) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason) : IntegrationEvent;

public sealed record OrderAmendedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<string> Changes) : IntegrationEvent;
