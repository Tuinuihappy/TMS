using Microsoft.EntityFrameworkCore;
using Tms.Orders.Application.Features.GetOrders;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Infrastructure.Persistence;

namespace Tms.Orders.Infrastructure.ReadServices;

/// <summary>
/// Thin read service — projects OrderDto directly in SQL.
/// ไม่ load full entity graph: select เฉพาะ columns ที่ OrderDto ต้องการ
/// ลด data transfer และ EF change tracking overhead
/// </summary>
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
                o.Items.Count,
                o.PickupAddress.Street + ", " + o.PickupAddress.Province,
                o.PickupAddress.Province,
                o.DropoffAddress.Street + ", " + o.DropoffAddress.Province,
                o.DropoffAddress.Province,
                o.PickupWindow != null ? o.PickupWindow.From : (DateTime?)null,
                o.PickupWindow != null ? o.PickupWindow.To : (DateTime?)null,
                o.DropoffWindow != null ? o.DropoffWindow.From : (DateTime?)null,
                o.DropoffWindow != null ? o.DropoffWindow.To : (DateTime?)null,
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
                o.Items.Count,
                o.PickupAddress.Street + ", " + o.PickupAddress.Province,
                o.PickupAddress.Province,
                o.DropoffAddress.Street + ", " + o.DropoffAddress.Province,
                o.DropoffAddress.Province,
                o.PickupWindow != null ? o.PickupWindow.From : (DateTime?)null,
                o.PickupWindow != null ? o.PickupWindow.To : (DateTime?)null,
                o.DropoffWindow != null ? o.DropoffWindow.From : (DateTime?)null,
                o.DropoffWindow != null ? o.DropoffWindow.To : (DateTime?)null,
                o.CreatedAt,
                o.UpdatedAt))
            .FirstOrDefaultAsync(ct);
}
