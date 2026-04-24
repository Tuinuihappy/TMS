using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.Planning.Application.Common.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

public sealed class OrderCancelledIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<OrderCancelledIntegrationEventHandler> logger) : INotificationHandler<OrderCancelledIntegrationEvent>
{
    public async Task Handle(OrderCancelledIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var planningOrder = await dbContext.PlanningOrders
            .FirstOrDefaultAsync(o => o.OrderId == notification.OrderId, cancellationToken);

        if (planningOrder is null)
        {
            logger.LogDebug("PlanningOrder for cancelled Order {OrderId} not found. Skipping.", notification.OrderId);
            return;
        }

        dbContext.PlanningOrders.Remove(planningOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Removed PlanningOrder for cancelled Order {OrderNumber}. Reason: {Reason}",
            notification.OrderNumber, notification.Reason);
    }
}
