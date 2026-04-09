using Microsoft.EntityFrameworkCore;
using Tms.Orders.Infrastructure.Persistence;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure;

/// <summary>
/// Implementation of IOrderQueryService backed by OrdersDbContext
/// ให้ Planning module เรียกใช้ผ่าน DI โดยไม่ต้อง reference Orders.Application/Domain โดยตรง
/// </summary>
public sealed class OrderQueryService(OrdersDbContext context) : IOrderQueryService
{
    public async Task<OrderSnapshot?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await context.TransportOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order is null ? null : MapSnapshot(order);
    }

    public async Task<List<OrderSnapshot>> GetOrdersByIdsAsync(
        IEnumerable<Guid> orderIds,
        CancellationToken ct = default)
    {
        var idList = orderIds.ToList();
        var orders = await context.TransportOrders
            .AsNoTracking()
            .Where(o => idList.Contains(o.Id))
            .ToListAsync(ct);

        return orders.Select(MapSnapshot).ToList();
    }

    private static OrderSnapshot MapSnapshot(Tms.Orders.Domain.Entities.TransportOrder o) => new(
        o.Id,
        o.OrderNumber,
        o.Status.ToString(),
        o.IsSplitChild,
        o.ParentOrderId,
        o.PickupAddress.Latitude,
        o.PickupAddress.Longitude,
        o.DropoffAddress.Latitude,
        o.DropoffAddress.Longitude,
        o.TotalWeight,
        o.TotalVolume,
        o.PickupWindow?.From,
        o.PickupWindow?.To,
        o.DropoffWindow?.From,
        o.DropoffWindow?.To);
}
