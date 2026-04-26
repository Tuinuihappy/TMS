using Tms.Orders.Application;
using MediatR;
using Tms.Orders.Domain.Events;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events.DomainEventHandlers;

public sealed class OrderCancelledDomainEventHandler(
    IOrdersOutboxWriter outbox) : INotificationHandler<OrderCancelledEvent>
{
    public Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        outbox.Stage(new OrderCancelledIntegrationEvent(
            notification.OrderId,
            notification.OrderNumber,
            notification.Reason));

        return Task.CompletedTask;
    }
}
