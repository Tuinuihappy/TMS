using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.SplitOrder;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

/// <summary>ระบุว่า Item ในแต่ละ Part จะใช้ Quantity เท่าไหร่</summary>
public sealed record ItemAllocationInput(Guid ItemId, int Quantity);

/// <summary>
/// Address input สำหรับ override dropoff (ใช้ใน Manual Split)
/// ถ้าไม่ระบุ child order จะ inherit จาก parent
/// </summary>
public sealed record SplitAddressInput(
    string Street,
    string SubDistrict,
    string District,
    string Province,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null);

/// <summary>Time window input</summary>
public sealed record SplitTimeWindowInput(DateTime From, DateTime To);

/// <summary>
/// แต่ละ Part = 1 child order ที่จะถูกสร้าง
/// </summary>
public sealed record SplitPartInput(
    List<ItemAllocationInput> Items,
    /// <summary>null = inherit จาก parent</summary>
    SplitAddressInput? OverrideDropoffAddress = null,
    /// <summary>null = inherit จาก parent</summary>
    SplitTimeWindowInput? OverrideDropoffWindow = null,
    string? Notes = null);

// ── Manual Split Command ─────────────────────────────────────────────────────

/// <summary>
/// POST /api/orders/{id}/split
/// Planner กำหนดเองว่าแต่ละ child order ได้ items อะไรบ้าง
/// รองรับ override DropoffAddress ได้ต่อ part
/// </summary>
public sealed record SplitOrderCommand(
    Guid OrderId,
    List<SplitPartInput> Parts,
    string? Reason = null) : ICommand<SplitOrderResult>;

/// <summary>
/// POST /api/orders/{id}/split/auto
/// ระบบแยกอัตโนมัติตาม capacity constraint
/// </summary>
public sealed record AutoSplitOrderCommand(
    Guid OrderId,
    decimal MaxWeightPerSplitKg,
    decimal MaxVolumePerSplitCBM = 0m,
    string? Reason = null) : ICommand<SplitOrderResult>;

// ── Result ───────────────────────────────────────────────────────────────────

public sealed record SplitOrderResult(
    Guid ParentOrderId,
    string ParentOrderNumber,
    List<SplitChildSummary> Children);

public sealed record SplitChildSummary(
    Guid ChildOrderId,
    string ChildOrderNumber,
    decimal TotalWeightKg,
    decimal TotalVolumeCBM,
    int ItemCount);
