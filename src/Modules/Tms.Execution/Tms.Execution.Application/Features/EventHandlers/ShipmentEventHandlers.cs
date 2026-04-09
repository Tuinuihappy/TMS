using MediatR;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Features.EventHandlers;

/// <summary>
/// เมื่อ Trip ถูก Dispatch → สร้าง Shipment 1 ใบต่อ Order (ไม่ใช่ต่อ Stop)
/// <br/>
/// Logic:
/// <br/>- Group stops by OrderId
/// <br/>- ต่อ Order: หา Dropoff stop (สำหรับ address snapshot + DropoffStopId)
/// <br/>- หา Pickup stop(s) แต่ไม่เก็บ FK เดี่ยว เพราะ 1 Order อาจมีหลาย Pickup Stops
/// <br/>- สร้าง Shipment 1 ใบ พร้อม Items (1 Item default ต่อ Order)
/// </summary>
public sealed class TripDispatchedShipmentCreator(IShipmentRepository repo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent notification, CancellationToken ct)
    {
        // Group stops by OrderId — 1 Shipment ต่อ Order
        var stopsByOrder = notification.Stops.GroupBy(s => s.OrderId);

        foreach (var group in stopsByOrder)
        {
            var orderId = group.Key;
            var stops = group.ToList();

            // หา Dropoff stop (ต้องมีอย่างน้อย 1)
            var dropoffStop = stops.FirstOrDefault(s =>
                string.Equals(s.StopType, "Dropoff", StringComparison.OrdinalIgnoreCase));

            if (dropoffStop is null)
            {
                // Order ที่ไม่มี Dropoff stop → ข้ามไป (ไม่สร้าง Shipment)
                // ในระบบจริงควร log warning
                continue;
            }

            // ตรวจว่า Shipment สำหรับ (TripId, OrderId) นี้มีอยู่แล้วหรือไม่ (idempotent)
            var existing = await repo.GetByTripAndOrderAsync(notification.TripId, orderId, ct);
            if (existing is not null) continue;

            var shipmentNumber = await repo.GenerateShipmentNumberAsync(ct);

            // Address snapshot มาจาก Dropoff stop (จุดส่งของ)
            var shipment = Shipment.Create(
                shipmentNumber,
                notification.TripId,
                orderId,
                dropoffStop.StopId,          // DropoffStopId
                notification.TenantId,
                dropoffStop.AddressName,
                dropoffStop.AddressStreet,
                dropoffStop.AddressProvince,
                dropoffStop.Latitude,
                dropoffStop.Longitude);

            // Default item — ในระบบจริงควร query จาก Orders module ผ่าน integration
            var item = ShipmentItem.Create(
                shipment.Id,
                $"Order {orderId}",
                expectedQty: 1,
                sku: null);
            shipment.AddItem(item);

            await repo.AddAsync(shipment, ct);
        }
    }
}

/// <summary>
/// เมื่อ Trip ถูก Cancel → ยกเลิก Shipments ที่ยังอยู่ใน Pending สำหรับ Trip นั้น
/// </summary>
public sealed class TripCancelledShipmentHandler(IShipmentRepository repo)
    : INotificationHandler<TripCancelledIntegrationEvent>
{
    public async Task Handle(TripCancelledIntegrationEvent notification, CancellationToken ct)
    {
        var shipments = await repo.GetByTripIdAsync(notification.TripId, ct);

        foreach (var shipment in shipments)
        {
            if (shipment.Status == Domain.Enums.ShipmentStatus.Pending)
            {
                shipment.RecordException(
                    $"Trip cancelled: {notification.Reason}",
                    "TRIP_CANCELLED");
                await repo.UpdateAsync(shipment, ct);
            }
        }
    }
}
