using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Application.Features.GetOrders;
using Tms.Orders.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.SplitOrder;

// ── Get Child Orders Query ────────────────────────────────────────────────────

public sealed record GetChildOrdersQuery(Guid ParentOrderId)
    : IQuery<List<ChildOrderDto>>;

/// <summary>Extended DTO ที่ include split metadata</summary>
public sealed record ChildOrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string Priority,
    decimal TotalWeightKg,
    decimal TotalVolumeCBM,
    int ItemCount,
    string PickupAddress,
    string DropoffAddress,
    Guid ParentOrderId,
    string? SplitReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed class GetChildOrdersHandler(IOrderRepository repo)
    : IQueryHandler<GetChildOrdersQuery, List<ChildOrderDto>>
{
    public async Task<List<ChildOrderDto>> Handle(
        GetChildOrdersQuery request, CancellationToken ct)
    {
        var children = await repo.GetChildOrdersAsync(request.ParentOrderId, ct);
        return children.Select(MapDto).ToList();
    }

    private static ChildOrderDto MapDto(TransportOrder o) => new(
        o.Id,
        o.OrderNumber,
        o.Status.ToString(),
        o.Priority.ToString(),
        o.TotalWeight,
        o.TotalVolume,
        o.Items.Count,
        o.PickupAddress.ToString(),
        o.DropoffAddress.ToString(),
        o.ParentOrderId!.Value,
        o.SplitReason,
        o.CreatedAt,
        o.UpdatedAt);
}
