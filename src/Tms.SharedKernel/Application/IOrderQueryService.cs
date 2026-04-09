namespace Tms.SharedKernel.Application;

/// <summary>
/// Cross-module interface: ให้ Planning module อ่าน Order data
/// โดยไม่ต้อง depend ตรงไปยัง Tms.Orders (ป้องกัน circular dependency)
/// Implement ใน Tms.Orders.Infrastructure → registered ใน DI
/// </summary>
public interface IOrderQueryService
{
    Task<OrderSnapshot?> GetOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<List<OrderSnapshot>> GetOrdersByIdsAsync(IEnumerable<Guid> orderIds, CancellationToken ct = default);
}

/// <summary>
/// Read-only snapshot ของ Order สำหรับใช้ใน Planning module
/// </summary>
public sealed record OrderSnapshot(
    Guid Id,
    string OrderNumber,
    string Status,
    bool IsSplitChild,
    Guid? ParentOrderId,
    double? PickupLat,
    double? PickupLng,
    double? DropoffLat,
    double? DropoffLng,
    decimal TotalWeightKg,
    decimal TotalVolumeCBM,
    DateTime? PickupWindowFrom,
    DateTime? PickupWindowTo,
    DateTime? DropoffWindowFrom,
    DateTime? DropoffWindowTo);
