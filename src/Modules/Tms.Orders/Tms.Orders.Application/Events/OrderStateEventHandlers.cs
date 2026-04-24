using MediatR;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events;

/// <summary>
/// เมื่อ Trip ถูก Dispatch → Order ที่เกี่ยวข้องทุก Order ใน Trip นั้น MarkAsInTransit()
/// Confirmed orders are first MarkAsPlanned() then MarkAsInTransit()
/// </summary>
public sealed class TripDispatchedOrderHandler(IOrderRepository repo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent ev, CancellationToken ct)
    {
        var orderIds = ev.Stops.Select(s => s.OrderId).Distinct().ToList();

        // 1 query to load all orders
        var orders = await repo.GetByIdsAsync(orderIds, ct);

        TransportOrder? lastUpdated = null;
        foreach (var order in orders)
        {
            if (order.Status == Domain.Enums.OrderStatus.Confirmed)
                order.MarkAsPlanned();

            if (order.Status == Domain.Enums.OrderStatus.Planned)
                order.MarkAsInTransit();

            lastUpdated = order;
        }

        // 1 SaveChanges for all orders
        if (lastUpdated is not null)
            await repo.UpdateAsync(lastUpdated, ct);
    }
}


/// <summary>
/// เมื่อ Shipment ถูก Delivered → ตรวจว่า Order นั้น Shipment ทุกใบ Complete หรือยัง
/// ถ้าใช่ → Order.Complete()
/// (เนื่องจาก TMS สร้าง 1 Shipment ต่อ Order → delivered 1 ใบ = order complete)
/// </summary>
public sealed class ShipmentDeliveredOrderHandler(IOrderRepository repo)
    : INotificationHandler<ShipmentDeliveredIntegrationEvent>
{
    public async Task Handle(ShipmentDeliveredIntegrationEvent ev, CancellationToken ct)
    {
        var order = await repo.GetByIdAsync(ev.OrderId, ct);
        if (order is null) return;

        if (order.Status == Domain.Enums.OrderStatus.InTransit)
        {
            order.Complete();
            await repo.UpdateAsync(order, ct);
        }
    }
}

/// <summary>
/// เมื่อ Trip ถูก Cancel → Order ที่ยังอยู่ใน Planned/InTransit → Cancel
/// </summary>
public sealed class TripCancelledOrderHandler(IOrderRepository repo)
    : INotificationHandler<TripCancelledIntegrationEvent>
{
    public async Task Handle(TripCancelledIntegrationEvent ev, CancellationToken ct)
    {
        var orderIds = ev.Stops.Select(s => s.OrderId).Distinct().ToList();
        var orders = await repo.GetByIdsAsync(orderIds, ct);

        TransportOrder? lastUpdated = null;
        foreach (var order in orders)
        {
            if (order.Status is Domain.Enums.OrderStatus.Planned
                             or Domain.Enums.OrderStatus.InTransit)
            {
                order.RevertToConfirmed();
                lastUpdated = order;
            }
        }

        if (lastUpdated is not null)
            await repo.UpdateAsync(lastUpdated, ct);
    }
}
