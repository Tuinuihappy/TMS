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
        var planningOrder = await dbContext.PlanningOrders
            .FirstOrDefaultAsync(o => o.OrderId == notification.OrderId, cancellationToken);

        if (planningOrder is null)
        {
            logger.LogDebug("PlanningOrder for amended Order {OrderId} not found. Skipping.", notification.OrderId);
            return;
        }

        // ดึงข้อมูล constraints ล่าสุดจาก Orders module
        var snapshot = await orderQueryService.GetOrderAsync(notification.OrderId, cancellationToken);
        if (snapshot is null)
        {
            logger.LogWarning("Order snapshot for {OrderId} not found after amendment. Skipping.", notification.OrderId);
            return;
        }

        planningOrder.UpdateConstraints(
            pickupLat: snapshot.PickupLat ?? 0,
            pickupLng: snapshot.PickupLng ?? 0,
            dropoffLat: snapshot.DropoffLat ?? 0,
            dropoffLng: snapshot.DropoffLng ?? 0,
            weight: snapshot.TotalWeightKg,
            volume: snapshot.TotalVolumeCBM,
            readyTime: snapshot.PickupWindowFrom,
            dueTime: snapshot.DropoffWindowTo);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated PlanningOrder constraints for amended Order {OrderNumber}. Changes: {Changes}",
            notification.OrderNumber, string.Join(", ", notification.Changes));
    }
}
