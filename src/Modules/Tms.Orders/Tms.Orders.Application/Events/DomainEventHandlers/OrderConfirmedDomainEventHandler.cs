using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Orders.Domain.Events;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events.DomainEventHandlers;

/// <summary>
/// เมื่อเกิด OrderConfirmedEvent (Domain Event) ให้ดึงข้อมูลที่เกี่ยวข้องกับ Routing Constraints
/// แล้วประกอบร่างเป็น OrderConfirmedIntegrationEvent ส่งขึ้น Outbox/RabbitMQ
/// เพื่อให้ Planning Module รับทราบเพื่อสร้าง PlanningOrder
/// </summary>
public sealed class OrderConfirmedDomainEventHandler(
    IOrderRepository orderRepository,
    IIntegrationEventPublisher eventPublisher,
    ILogger<OrderConfirmedDomainEventHandler> logger) : INotificationHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(notification.OrderId, cancellationToken);
        
        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found while handling OrderConfirmedEvent.", notification.OrderId);
            return;
        }

        // คิวรีข้อมูลโครงสร้าง Constraints จาก TransportOrder
        // ที่อยู่จุดรับ-ส่ง
        var pickupLat = order.PickupAddress?.Latitude ?? 0;
        var pickupLng = order.PickupAddress?.Longitude ?? 0;
        var dropoffLat = order.DropoffAddress?.Latitude ?? 0;
        var dropoffLng = order.DropoffAddress?.Longitude ?? 0;

        // นำหนักและปริมาตร (รวมจากทุกๆ Items)
        var totalWeight = order.TotalWeight;
        var totalVolume = order.TotalVolume;

        // ข้อกำหนดเงื่อนไขด้านเวลา
        var readyTime = order.PickupWindow?.From;
        var dueTime = order.DropoffWindow?.To ?? order.DropoffWindow?.From;

        // แปลงร่างเป็น Integration Event (Cross Module Shared Mapping)
        var integrationEvent = new OrderConfirmedIntegrationEvent(
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            CustomerId: order.CustomerId,
            PickupLatitude: pickupLat,
            PickupLongitude: pickupLng,
            DropoffLatitude: dropoffLat,
            DropoffLongitude: dropoffLng,
            TotalWeight: totalWeight,
            TotalVolume: totalVolume,
            ReadyTime: readyTime,
            DueTime: dueTime
        );

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        logger.LogInformation("Successfully mapped and published OrderConfirmedIntegrationEvent for Order {OrderNumber} with Constraints: W={Weight}, V={Volume}", 
                              order.OrderNumber, totalWeight, totalVolume);
    }
}
