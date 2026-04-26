using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Application.Common.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

/// <summary>
/// เมื่อ Order Confirmed → สร้าง 1 PlanningOrder ต่อ OrderStop เข้า Planning Pool
/// </summary>
public sealed class OrderConfirmedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<OrderConfirmedIntegrationEventHandler> logger) : INotificationHandler<OrderConfirmedIntegrationEvent>
{
    public async Task Handle(OrderConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Planning received OrderConfirmed for {OrderNumber} — {StopCount} stop(s)",
            notification.OrderNumber, notification.Stops.Count);

        // Idempotency: ดึง StopId ที่มี PlanningOrder อยู่แล้ว
        var existingStopIds = await dbContext.PlanningOrders
            .Where(o => o.OrderId == notification.OrderId)
            .Select(o => o.OrderStopId)
            .ToHashSetAsync(cancellationToken);

        var newOrders = new List<PlanningOrder>();

        foreach (var stop in notification.Stops)
        {
            if (existingStopIds.Contains(stop.StopId))
            {
                logger.LogWarning(
                    "PlanningOrder for Stop {StopId} already exists. Skipping.", stop.StopId);
                continue;
            }

            newOrders.Add(PlanningOrder.Create(
                orderId: notification.OrderId,
                orderStopId: stop.StopId,
                orderNumber: notification.OrderNumber,
                tenantId: notification.TenantId,
                pickupLat: stop.PickupLatitude,
                pickupLng: stop.PickupLongitude,
                dropoffLat: stop.DropoffLatitude,
                dropoffLng: stop.DropoffLongitude,
                weight: stop.WeightKg,
                volume: stop.VolumeCBM,
                readyTime: stop.ReadyTime,
                dueTime: stop.DueTime));
        }

        if (newOrders.Count == 0) return;

        dbContext.PlanningOrders.AddRange(newOrders);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Inserted {Count} PlanningOrder(s) for Order {OrderNumber}.",
            newOrders.Count, notification.OrderNumber);
    }
}
