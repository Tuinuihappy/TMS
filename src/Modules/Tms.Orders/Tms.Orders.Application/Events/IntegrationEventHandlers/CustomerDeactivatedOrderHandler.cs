using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events.IntegrationEventHandlers;

public sealed class CustomerDeactivatedOrderHandler(
    IOrderRepository repo,
    ILogger<CustomerDeactivatedOrderHandler> logger)
    : INotificationHandler<CustomerDeactivatedIntegrationEvent>
{
    public async Task Handle(
        CustomerDeactivatedIntegrationEvent notification, CancellationToken ct)
    {
        var orders = await repo.GetActiveByCustomerIdAsync(notification.CustomerId, ct);

        if (orders.Count == 0)
        {
            logger.LogDebug(
                "No active orders found for deactivated customer {CustomerCode}.",
                notification.CustomerCode);
            return;
        }

        foreach (var order in orders)
            order.Cancel($"Customer {notification.CustomerCode} has been deactivated.");

        await repo.UpdateAsync(orders[^1], ct);

        logger.LogInformation(
            "Cancelled {Count} active order(s) for deactivated customer {CustomerCode}.",
            orders.Count, notification.CustomerCode);
    }
}
