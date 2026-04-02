using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Tracking.Application.Features;
using Tms.SharedKernel.Application;

namespace Tms.Tracking.Infrastructure;

/// <summary>
/// Background worker ที่รัน Geofence check ทุก 30 วินาที
/// ดึง current vehicle states แล้วเช็คว่าคันไหนเข้า GeoZone บ้าง
/// </summary>
public sealed class GeofenceBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GeofenceBackgroundWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GeofenceBackgroundWorker started. Checking every {Interval}s.", CheckInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Geofence check failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        logger.LogInformation("GeofenceBackgroundWorker stopping.");
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        var tenantIds = await GetActiveTenantIdsAsync(scope.ServiceProvider, ct);

        foreach (var tenantId in tenantIds)
        {
            await mediator.Send(new RunGeofenceChecksCommand(tenantId), ct);
        }

        logger.LogDebug("Geofence check completed for {Count} tenant(s).", tenantIds.Count);
    }

    /// <summary>
    /// ดึง TenantId ทั้งหมดจาก CurrentVehicleStates ที่มีอยู่ใน DB
    /// </summary>
    private static async Task<List<Guid>> GetActiveTenantIdsAsync(
        IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<Domain.Interfaces.ICurrentVehicleStateRepository>();
        var states = await repo.GetAllAsync(tenantId: null, ct);
        return states.Select(s => s.TenantId).Distinct().ToList();
    }
}
