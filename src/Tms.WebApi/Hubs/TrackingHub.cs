using Microsoft.AspNetCore.SignalR;

namespace Tms.WebApi.Hubs;

/// <summary>
/// SignalR Hub สำหรับ Live Tracking Map (Phase 2)
/// Clients connect แล้ว join tenant group เพื่อรับ real-time vehicle position updates
/// </summary>
public sealed class TrackingHub : Hub
{
    /// <summary>
    /// Client เรียกเพื่อ join group ตาม tenantId — จะได้รับ updates เฉพาะ tenant นั้น
    /// </summary>
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
    }

    /// <summary>
    /// Client เรียกเพื่อ leave group
    /// </summary>
    public async Task LeaveTenantGroup(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");
    }
}
