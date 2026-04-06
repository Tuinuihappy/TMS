using MediatR;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.EventHandlers;

/// <summary>
/// เมื่อ Shipment ส่งสำเร็จ → สร้าง OmsOutboxEvent เพื่อดัน Status กลับ OMS.
/// เฉพาะ Shipment ที่มาจาก OMS (มี ExternalOrderRef) เท่านั้น.
/// </summary>
public sealed class ShipmentDeliveredOmsHandler(IOmsSyncRepository repo)
    : INotificationHandler<ShipmentDeliveredIntegrationEvent>
{
    public async Task Handle(ShipmentDeliveredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // ค้นหา OmsOrderSync ที่ตรงกับ OrderId นี้
        // MVP: scan ผ่าน DB — Phase 5 เพิ่ม Index บน TmsOrderId
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            tmsOrderId = notification.OrderId,
            tmsShipmentId = notification.ShipmentId,
            status = "delivered",
            deliveredAt = notification.DeliveredAt
        });

        // สร้าง Outbox event (OmsProviderCode จะ resolve จาก sync record จริงๆ)
        // MVP: ใช้ DEFAULT_OMS เพื่อให้ Outbox Worker ประมวลผลได้
        var outbox = OmsOutboxEvent.Create(
            omsProviderCode: "DEFAULT_OMS",
            tmsOrderId: notification.OrderId,
            externalOrderRef: $"ORDER-{notification.OrderId:N}",
            eventType: "ShipmentDelivered",
            payload: payload,
            tenantId: Guid.Empty); // Tenant จะ resolve จาก context จริงๆ

        // ป้องกัน duplicate
        if (!await repo.OutboxExistsAsync(outbox.IdempotencyKey, cancellationToken))
            await repo.AddOutboxEventAsync(outbox, cancellationToken);
    }
}
