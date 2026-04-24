using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.GetOrders;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string Status,
    string Priority,
    decimal TotalWeight,
    decimal TotalVolumeCBM,
    int ItemCount,
    string PickupAddress,
    string PickupProvince,
    string DropoffAddress,
    string DropoffProvince,
    DateTime? PickupWindowFrom,
    DateTime? PickupWindowTo,
    DateTime? DropoffWindowFrom,
    DateTime? DropoffWindowTo,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record GetOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? CustomerId = null
) : IQuery<PagedResult<OrderDto>>;

public sealed class GetOrdersHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await orderRepository.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.CustomerId,
            cancellationToken);

        var dtos = items.Select(MapDto).ToList();
        return PagedResult<OrderDto>.Create(dtos, totalCount, request.Page, request.PageSize);
    }

    internal static OrderDto MapDto(Domain.Entities.TransportOrder o) => new(
        o.Id,
        o.OrderNumber,
        o.CustomerId,
        o.Status.ToString(),
        o.Priority.ToString(),
        o.TotalWeight,
        o.TotalVolumeCBM,
        o.Items.Count,
        o.PickupAddress.ToString(),
        o.PickupAddress.Province,
        o.DropoffAddress.ToString(),
        o.DropoffAddress.Province,
        o.PickupWindow?.From,
        o.PickupWindow?.To,
        o.DropoffWindow?.From,
        o.DropoffWindow?.To,
        o.CreatedAt,
        o.UpdatedAt);
}

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;

public sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var o = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        return o is null ? null : GetOrdersHandler.MapDto(o);
    }
}
