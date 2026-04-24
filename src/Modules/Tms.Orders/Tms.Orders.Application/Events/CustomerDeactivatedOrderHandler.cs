using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Events;

/// <summary>
/// เมื่อ Customer ถูก deactivate → cancel orders ที่ยังเป็น Draft/Confirmed ทั้งหมด
/// OrderCancelledEvent (domain event) จะ trigger OrderCancelledDomainEventHandler
/// ซึ่งจะ stage OrderCancelledIntegrationEvent ให้ Planning module รับทราบอัตโนมัติ
/// </summary>
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

        // UpdateAsync ของ order ตัวสุดท้าย → SaveChangesAsync เดียวบันทึกทุก order
        // DomainEventDispatcher ใน OrdersDbContext จะเก็บ OrderCancelledEvent ทุกตัวเข้า Outbox
        await repo.UpdateAsync(orders[^1], ct);

        logger.LogInformation(
            "Cancelled {Count} active order(s) for deactivated customer {CustomerCode}.",
            orders.Count, notification.CustomerCode);
    }
}
