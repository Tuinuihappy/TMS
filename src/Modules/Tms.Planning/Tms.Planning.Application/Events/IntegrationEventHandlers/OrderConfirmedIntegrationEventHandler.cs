using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Application.Common.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

/// <summary>
/// Trigger แบบ A (Real-time): เมื่อ Order Management ส่งสถานะว่า Order เป็น Confirmed แล้ว
/// 모ดูล Planning จะรับทราบ และสร้าง PlanningOrder (Read Model) เป็น Unplanned มารอในตะกร้า
/// หากเป็น Order VIP หรือ Priority สูง สามารถจุดชนวน Auto Plan ต่อไปได้ทันที
/// </summary>
public sealed class OrderConfirmedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<OrderConfirmedIntegrationEventHandler> logger) : INotificationHandler<OrderConfirmedIntegrationEvent>
{
    public async Task Handle(OrderConfirmedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Planning Module received OrderConfirmedIntegrationEvent for Order: {OrderNumber}", notification.OrderNumber);

        // 1. เช็คว่าเคสดึงเข้ามาซ้ำไหม (Idempotency พื้นฐาน)
        var exists = dbContext.PlanningOrders.Any(o => o.OrderId == notification.OrderId);
        if (exists)
        {
            logger.LogWarning("PlanningOrder for {OrderId} already exists. Skipping.", notification.OrderId);
            return;
        }

        // 2. Insert เข้า Planning Schema โดยอาศัยข้อมูล Constraints ที่ติดมากับ Event จาก Orders Module
        var planningOrder = PlanningOrder.Create(
            orderId: notification.OrderId,
            orderNumber: notification.OrderNumber,
            tenantId: Guid.NewGuid(), // ควรเปลี่ยนเป็นรับจาก Tenant Context/Header แท้จริง
            pickupLat: notification.PickupLatitude,
            pickupLng: notification.PickupLongitude,
            dropoffLat: notification.DropoffLatitude,
            dropoffLng: notification.DropoffLongitude,
            weight: notification.TotalWeight,
            volume: notification.TotalVolume,
            readyTime: notification.ReadyTime,
            dueTime: notification.DueTime
        );

        dbContext.PlanningOrders.Add(planningOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully inserted PlanningOrder {OrderNumber} as Unplanned into Planning Pool.", notification.OrderNumber);
        
        // --- 
        // Real-Time Trigger (Optional): 
        // ถ้าระบบต้องการให้ Plan ทันทีแบบไม่ต้องรอ Batch ถัดไป ก็สามารถโยนคำสั่ง StartAutoOptimizationCommand ได้ตรงนี้
        // ---
    }
}
