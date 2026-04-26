using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.Planning.Application.Common.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

public sealed class OrderAmendedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    IOrderQueryService orderQueryService,
    ILogger<OrderAmendedIntegrationEventHandler> logger) : INotificationHandler<OrderAmendedIntegrationEvent>
{
    public async Task Handle(OrderAmendedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var planningOrders = await dbContext.PlanningOrders
            .Where(o => o.OrderId == notification.OrderId)
            .ToListAsync(cancellationToken);

        if (planningOrders.Count == 0)
        {
            logger.LogDebug("No PlanningOrders found for amended Order {OrderId}. Skipping.", notification.OrderId);
            return;
        }

        var snapshot = await orderQueryService.GetOrderAsync(notification.OrderId, cancellationToken);
        if (snapshot is null)
        {
            logger.LogWarning("Order snapshot for {OrderId} not found after amendment. Skipping.", notification.OrderId);
            return;
        }

        var stopMap = snapshot.Stops.ToDictionary(s => s.StopId);

        foreach (var po in planningOrders)
        {
            if (!stopMap.TryGetValue(po.OrderStopId, out var stop)) continue;

            po.UpdateConstraints(
                pickupLat: stop.PickupLat,
                pickupLng: stop.PickupLng,
                dropoffLat: stop.DropoffLat,
                dropoffLng: stop.DropoffLng,
                weight: stop.WeightKg,
                volume: stop.VolumeCBM,
                readyTime: stop.ReadyTime,
                dueTime: stop.DueTime);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated {Count} PlanningOrder constraint(s) for amended Order {OrderNumber}. Changes: {Changes}",
            planningOrders.Count, notification.OrderNumber, string.Join(", ", notification.Changes));
    }
}
