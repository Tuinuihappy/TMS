using MediatR;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Execution.Application.Features.EventHandlers;

/// <summary>
/// เมื่อรถเข้า GeoZone ที่ผูกกับ LocationId
/// ระบบจะหา Shipment ที่ AddressLatitude/Longitude ตรงกับ Location นั้น
/// แล้ว auto-Arrive() เพื่อไม่ให้ Driver ต้องกดปุ่มเอง
/// </summary>
public sealed class VehicleEnteredZoneShipmentArriveHandler(IShipmentRepository repo)
    : INotificationHandler<VehicleEnteredZoneIntegrationEvent>
{
    public async Task Handle(VehicleEnteredZoneIntegrationEvent notification, CancellationToken ct)
    {
        // หา Shipments ของ Trip ที่ยังไม่ Arrived และเป็น vehicle นี้
        // (ใช้ LocationId เทียบกับ shipment destination — ต้องค้นหาจาก TenantId + pending status)
        var allShipments = await repo.GetByTenantPendingAsync(notification.TenantId, ct);

        foreach (var shipment in allShipments)
        {
            // ตรวจสอบว่า Shipment destination ตรงกับ LocationId ของ Zone นี้
            // (Phase 2: เราเปรียบเทียบง่ายๆ โดย AddressLocationId ที่ฝังไว้ใน Shipment)
            if (shipment.DestinationLocationId == notification.LocationId
                && shipment.Status is Domain.Enums.ShipmentStatus.PickedUp
                    or Domain.Enums.ShipmentStatus.InTransit)
            {
                shipment.Arrive();
                await repo.UpdateAsync(shipment, ct);
            }
        }
    }
}
