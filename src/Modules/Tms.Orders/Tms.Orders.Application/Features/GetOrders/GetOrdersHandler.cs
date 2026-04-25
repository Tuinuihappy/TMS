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
    Guid? CustomerId = null,
    Guid TenantId = default
) : IQuery<PagedResult<OrderDto>>;

public sealed class GetOrdersHandler(IOrderReadService readService)
    : IQueryHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<PagedResult<OrderDto>> Handle(
        GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await readService.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.CustomerId,
            request.TenantId,
            cancellationToken);

        return PagedResult<OrderDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}

public sealed record GetOrderByIdQuery(Guid OrderId, Guid TenantId) : IQuery<OrderDto?>;

public sealed class GetOrderByIdHandler(IOrderReadService readService)
    : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        => await readService.GetByIdAsync(request.OrderId, request.TenantId, cancellationToken);
}
