using MediatR;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events.IntegrationEventHandlers;

public sealed class TripDispatchedOrderHandler(IOrderRepository repo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent ev, CancellationToken ct)
    {
        var orderIds = ev.Stops.Select(s => s.OrderId).Distinct().ToList();
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

        if (lastUpdated is not null)
            await repo.UpdateAsync(lastUpdated, ct);
    }
}

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
