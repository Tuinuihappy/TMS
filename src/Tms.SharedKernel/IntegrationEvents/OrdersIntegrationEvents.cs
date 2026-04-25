using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    Guid TenantId,
    string Priority,
    double PickupLatitude,
    double PickupLongitude,
    string PickupProvince,
    double DropoffLatitude,
    double DropoffLongitude,
    string DropoffProvince,
    decimal TotalWeight,
    decimal TotalVolumeCBM,
    int ItemCount,
    bool HasDangerousGoods,
    DateTime? ReadyTime,
    DateTime? DueTime,
    DateTime? PickupWindowFrom,
    DateTime? PickupWindowTo,
    DateTime? DropoffWindowFrom,
    DateTime? DropoffWindowTo) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason) : IntegrationEvent;

public sealed record OrderAmendedIntegrationEvent(
    Guid OrderId,
    string OrderNumber,
    IReadOnlyList<string> Changes) : IntegrationEvent;
