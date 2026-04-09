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
        var orderIds = ev.Stops.Select(s => s.OrderId).Distinct();
        foreach (var orderId in orderIds)
        {
            var order = await repo.GetByIdAsync(orderId, ct);
            if (order is null) continue;

            // Confirmed → auto plan then start transit (skip manual planning for auto-dispatched trips)
            if (order.Status == Domain.Enums.OrderStatus.Confirmed)
                order.MarkAsPlanned();

            if (order.Status == Domain.Enums.OrderStatus.Planned)
                order.MarkAsInTransit();

            await repo.UpdateAsync(order, ct);
        }
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
        // TripCancelledIntegrationEvent does not carry OrderIds directly
        // → handled at Shipment level; Order stays InTransit until re-planned
        // This handler is intentionally a no-op for now — future: re-queue for re-planning
        await Task.CompletedTask;
    }
}
