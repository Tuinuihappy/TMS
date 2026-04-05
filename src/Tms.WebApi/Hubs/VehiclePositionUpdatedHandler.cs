using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Tms.WebApi.Hubs;

/// <summary>
/// MediatR Notification ที่ถูก publish หลัง GPS ingest สำเร็จ
/// เพื่อ push ตำแหน่งรถล่าสุดไปยัง SignalR clients
/// </summary>
public sealed record VehiclePositionUpdatedNotification(
    Guid VehicleId,
    Guid TenantId,
    double Lat,
    double Lng,
    decimal SpeedKmh,
    DateTime Timestamp) : INotification;

/// <summary>
/// Handler ที่ push vehicle position ไปยัง SignalR tenant group
/// </summary>
public sealed class VehiclePositionUpdatedHandler(
    IHubContext<TrackingHub> hubContext)
    : INotificationHandler<VehiclePositionUpdatedNotification>
{
    public async Task Handle(VehiclePositionUpdatedNotification notification, CancellationToken ct)
    {
        await hubContext.Clients
            .Group($"tenant-{notification.TenantId}")
            .SendAsync("VehiclePositionUpdated", new
            {
                notification.VehicleId,
                notification.Lat,
                notification.Lng,
                notification.SpeedKmh,
                notification.Timestamp
            }, ct);
    }
}
