using Microsoft.EntityFrameworkCore;
using Tms.Orders.Application.Features.GetOrders;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Infrastructure.Persistence;

namespace Tms.Orders.Infrastructure.ReadServices;

public sealed class OrderReadService(OrdersDbContext ctx) : IOrderReadService
{
    public async Task<(IReadOnlyList<OrderDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? status, Guid? customerId, Guid tenantId, CancellationToken ct)
    {
        var query = ctx.TransportOrders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);
            else
                return ([], 0);
        }

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.Status.ToString(),
                o.Priority.ToString(),
                o.TotalWeight,
                o.TotalVolumeCBM,
                o.Stops.Count,
                o.Stops.Sum(s => s.Items.Count),
                o.Stops.OrderBy(s => s.Sequence).Select(s => new OrderStopSummaryDto(
                    s.Id,
                    s.Sequence,
                    s.PickupAddress.Street + ", " + s.PickupAddress.Province,
                    s.PickupAddress.Province,
                    s.DropoffAddress.Street + ", " + s.DropoffAddress.Province,
                    s.DropoffAddress.Province,
                    s.PickupWindow != null ? s.PickupWindow.From : (DateTime?)null,
                    s.PickupWindow != null ? s.PickupWindow.To : (DateTime?)null,
                    s.DropoffWindow != null ? s.DropoffWindow.From : (DateTime?)null,
                    s.DropoffWindow != null ? s.DropoffWindow.To : (DateTime?)null
                )).ToList(),
                o.CreatedAt,
                o.UpdatedAt))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid orderId, Guid tenantId, CancellationToken ct) =>
        await ctx.TransportOrders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.TenantId == tenantId)
            .Select(o => new OrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerId,
                o.Status.ToString(),
                o.Priority.ToString(),
                o.TotalWeight,
                o.TotalVolumeCBM,
                o.Stops.Count,
                o.Stops.Sum(s => s.Items.Count),
                o.Stops.OrderBy(s => s.Sequence).Select(s => new OrderStopSummaryDto(
                    s.Id,
                    s.Sequence,
                    s.PickupAddress.Street + ", " + s.PickupAddress.Province,
                    s.PickupAddress.Province,
                    s.DropoffAddress.Street + ", " + s.DropoffAddress.Province,
                    s.DropoffAddress.Province,
                    s.PickupWindow != null ? s.PickupWindow.From : (DateTime?)null,
                    s.PickupWindow != null ? s.PickupWindow.To : (DateTime?)null,
                    s.DropoffWindow != null ? s.DropoffWindow.From : (DateTime?)null,
                    s.DropoffWindow != null ? s.DropoffWindow.To : (DateTime?)null
                )).ToList(),
                o.CreatedAt,
                o.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
