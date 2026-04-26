using Tms.Orders.Application;
using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Orders.Domain.Events;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events.DomainEventHandlers;

public sealed class OrderConfirmedDomainEventHandler(
    IOrderRepository orderRepository,
    IOrdersOutboxWriter outbox,
    ILogger<OrderConfirmedDomainEventHandler> logger) : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(notification.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found while handling OrderConfirmedEvent.", notification.OrderId);
            return;
        }

        var stopSnapshots = order.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new OrderStopSnapshot(
                StopId: s.Id,
                Sequence: s.Sequence,
                PickupLatitude: s.PickupAddress?.Latitude ?? 0,
                PickupLongitude: s.PickupAddress?.Longitude ?? 0,
                PickupProvince: s.PickupAddress?.Province ?? string.Empty,
                DropoffLatitude: s.DropoffAddress?.Latitude ?? 0,
                DropoffLongitude: s.DropoffAddress?.Longitude ?? 0,
                DropoffProvince: s.DropoffAddress?.Province ?? string.Empty,
                WeightKg: s.StopTotalWeight,
                VolumeCBM: s.StopTotalVolumeCBM,
                ReadyTime: s.PickupWindow?.From,
                DueTime: s.DropoffWindow?.To,
                PickupWindowFrom: s.PickupWindow?.From,
                PickupWindowTo: s.PickupWindow?.To,
                DropoffWindowFrom: s.DropoffWindow?.From,
                DropoffWindowTo: s.DropoffWindow?.To))
            .ToList();

        var allItems = order.Stops.SelectMany(s => s.Items).ToList();

        outbox.Stage(new OrderConfirmedIntegrationEvent(
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            CustomerId: order.CustomerId,
            TenantId: order.TenantId,
            Priority: order.Priority.ToString(),
            TotalWeight: order.TotalWeight,
            TotalVolumeCBM: order.TotalVolumeCBM,
            ItemCount: allItems.Count,
            HasDangerousGoods: allItems.Any(i => i.IsDangerousGoods),
            Stops: stopSnapshots));

        logger.LogInformation(
            "Staged OrderConfirmedIntegrationEvent for Order {OrderNumber}: W={Weight}, V={Volume}, Stops={StopCount}, Items={ItemCount}",
            order.OrderNumber, order.TotalWeight, order.TotalVolumeCBM, stopSnapshots.Count, allItems.Count);
    }
}
