using MediatR;
using Tms.Orders.Application.Features.SplitOrder;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Events;

/// <summary>
/// Bridge handler: รับ CrossModuleAutoSplitRequest จาก Planning module ผ่าน MediatR in-process
/// แล้วส่งต่อไปยัง AutoSplitOrderHandler ใน Orders module
/// ทำให้ Planning ไม่ต้อง reference Orders.Application โดยตรง
/// </summary>
public sealed class CrossModuleAutoSplitHandler(ISender sender)
    : IRequestHandler<CrossModuleAutoSplitRequest, CrossModuleAutoSplitResult>
{
    public async Task<CrossModuleAutoSplitResult> Handle(
        CrossModuleAutoSplitRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new AutoSplitOrderCommand(
                request.OrderId,
                request.MaxWeightPerSplitKg,
                request.MaxVolumePerSplitCBM,
                "RouteConstraint"),
            ct);

        return new CrossModuleAutoSplitResult(
            result.ParentOrderId,
            result.ParentOrderNumber,
            result.Children.Select(c => c.ChildOrderId).ToList());
    }
}
