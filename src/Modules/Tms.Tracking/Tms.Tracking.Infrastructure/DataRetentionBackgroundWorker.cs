using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Tracking.Domain.Interfaces;

namespace Tms.Tracking.Infrastructure;

/// <summary>
/// Background worker ที่ลบข้อมูล GPS เก่ากว่า 90 วัน — รันทุกสัปดาห์
/// ตาม spec phase2_tracking: Data Retention cleanup
/// </summary>
public sealed class DataRetentionBackgroundWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DataRetentionBackgroundWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromDays(7);
    private const int RetentionDays = 90;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "DataRetentionBackgroundWorker started. Cleaning VehiclePositions older than {Days} days, every {Interval} days.",
            RetentionDays, CheckInterval.TotalDays);

        // Wait a bit on startup before first run
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Data retention cleanup failed.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IVehiclePositionRepository>();

        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var deletedCount = await repo.DeleteOlderThanAsync(cutoff, ct);

        logger.LogInformation(
            "Data retention: deleted {Count} VehiclePosition records older than {CutoffDate:yyyy-MM-dd}.",
            deletedCount, cutoff);
    }
}
