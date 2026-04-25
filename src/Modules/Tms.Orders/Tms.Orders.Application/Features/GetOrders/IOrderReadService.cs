namespace Tms.Orders.Application.Features.GetOrders;

/// <summary>
/// Read-side service for Order list/detail queries.
/// Uses LINQ projection to select only columns needed by the DTO — avoids loading full entity graph.
/// Implemented in Infrastructure with direct EF Core access.
/// </summary>
public interface IOrderReadService
{
    Task<(IReadOnlyList<OrderDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? status, Guid? customerId, Guid tenantId, CancellationToken ct);

    Task<OrderDto?> GetByIdAsync(Guid orderId, Guid tenantId, CancellationToken ct);
}
