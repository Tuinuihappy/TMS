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

        outbox.Stage(new OrderConfirmedIntegrationEvent(
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            CustomerId: order.CustomerId,
            TenantId: order.TenantId,
            Priority: order.Priority.ToString(),
            PickupLatitude: order.PickupAddress?.Latitude ?? 0,
            PickupLongitude: order.PickupAddress?.Longitude ?? 0,
            PickupProvince: order.PickupAddress?.Province ?? string.Empty,
            DropoffLatitude: order.DropoffAddress?.Latitude ?? 0,
            DropoffLongitude: order.DropoffAddress?.Longitude ?? 0,
            DropoffProvince: order.DropoffAddress?.Province ?? string.Empty,
            TotalWeight: order.TotalWeight,
            TotalVolumeCBM: order.TotalVolumeCBM,
            ItemCount: order.Items.Count,
            HasDangerousGoods: order.Items.Any(i => i.IsDangerousGoods),
            ReadyTime: order.PickupWindow?.From,
            DueTime: order.DropoffWindow?.To,
            PickupWindowFrom: order.PickupWindow?.From,
            PickupWindowTo: order.PickupWindow?.To,
            DropoffWindowFrom: order.DropoffWindow?.From,
            DropoffWindowTo: order.DropoffWindow?.To));

        logger.LogInformation(
            "Staged OrderConfirmedIntegrationEvent for Order {OrderNumber}: W={Weight}, V={Volume}, Items={Count}",
            order.OrderNumber, order.TotalWeight, order.TotalVolumeCBM, order.Items.Count);
    }
}
