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
    decimal TotalVolume,
    int ItemCount,
    string PickupAddress,
    string DropoffAddress,
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

        var dtos = items.Select(o => new OrderDto(
            o.Id,
            o.OrderNumber,
            o.CustomerId,
            o.Status.ToString(),
            o.Priority.ToString(),
            o.TotalWeight,
            o.TotalVolume,
            o.Items.Count,
            o.PickupAddress.ToString(),
            o.DropoffAddress.ToString(),
            o.CreatedAt,
            o.UpdatedAt)).ToList();

        return PagedResult<OrderDto>.Create(dtos, totalCount, request.Page, request.PageSize);
    }
}

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;

public sealed class GetOrderByIdHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var o = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (o is null) return null;

        return new OrderDto(
            o.Id, o.OrderNumber, o.CustomerId,
            o.Status.ToString(), o.Priority.ToString(),
            o.TotalWeight, o.TotalVolume, o.Items.Count,
            o.PickupAddress.ToString(), o.DropoffAddress.ToString(),
            o.CreatedAt, o.UpdatedAt);
    }
}
