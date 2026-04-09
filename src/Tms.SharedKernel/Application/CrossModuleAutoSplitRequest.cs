using MediatR;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Cross-module auto split request — defined in SharedKernel เพื่อให้ทั้ง
/// Planning.Application (sender) และ Orders.Application (handler) reference ได้โดยไม่เกิด circular deps
/// </summary>
public sealed record CrossModuleAutoSplitRequest(
    Guid OrderId,
    decimal MaxWeightPerSplitKg,
    decimal MaxVolumePerSplitCBM = 0m) : IRequest<CrossModuleAutoSplitResult>;

public sealed record CrossModuleAutoSplitResult(
    Guid ParentOrderId,
    string ParentOrderNumber,
    List<Guid> ChildOrderIds);
