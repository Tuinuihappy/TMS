using Microsoft.EntityFrameworkCore;
using Tms.Orders.Infrastructure.Persistence;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Infrastructure.ReadServices;

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
        o.TotalWeight,
        o.TotalVolumeCBM,
        o.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new OrderStopConstraint(
                StopId: s.Id,
                Sequence: s.Sequence,
                PickupLat: s.PickupAddress?.Latitude ?? 0,
                PickupLng: s.PickupAddress?.Longitude ?? 0,
                DropoffLat: s.DropoffAddress?.Latitude ?? 0,
                DropoffLng: s.DropoffAddress?.Longitude ?? 0,
                WeightKg: s.StopTotalWeight,
                VolumeCBM: s.StopTotalVolumeCBM,
                ReadyTime: s.PickupWindow?.From,
                DueTime: s.DropoffWindow?.To,
                PickupWindowFrom: s.PickupWindow?.From,
                PickupWindowTo: s.PickupWindow?.To,
                DropoffWindowFrom: s.DropoffWindow?.From,
                DropoffWindowTo: s.DropoffWindow?.To))
            .ToList()
            .AsReadOnly());
}
