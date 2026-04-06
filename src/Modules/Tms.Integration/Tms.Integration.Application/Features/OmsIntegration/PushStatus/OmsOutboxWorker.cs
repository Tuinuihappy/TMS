using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;

namespace Tms.Integration.Application.Features.OmsIntegration.PushStatus;

/// <summary>
/// Outbox Worker — ดึง OmsOutboxEvents ที่ Pending แล้วส่งผ่าน IOmsCallbackSender.
/// IOmsCallbackSender implement ใน Infrastructure เพื่อให้ Application ไม่ต้องรู้จัก HttpClient.
/// </summary>
public sealed class OmsOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OmsOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OMS Outbox Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OMS Outbox Worker error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOmsSyncRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IOmsCallbackSender>();

        var pendingEvents = await repo.GetPendingOutboxAsync(batchSize: 50, ct);

        foreach (var outbox in pendingEvents)
        {
            try
            {
                var success = await sender.SendAsync(
                    outbox.OmsProviderCode, outbox.Payload, ct);

                if (success)
                {
                    outbox.Status = OutboxStatus.Sent;
                    outbox.SentAt = DateTime.UtcNow;
                    logger.LogInformation("OMS Outbox {Id} sent successfully.", outbox.Id);
                }
                else
                {
                    outbox.RetryCount++;
                    if (outbox.RetryCount >= 5) outbox.Status = OutboxStatus.Failed;
                    logger.LogWarning("OMS Outbox {Id} send failed.", outbox.Id);
                }
            }
            catch (Exception ex)
            {
                outbox.RetryCount++;
                if (outbox.RetryCount >= 5) outbox.Status = OutboxStatus.Failed;
                logger.LogWarning(ex, "OMS Outbox {Id} exception on attempt {Retry}.", outbox.Id, outbox.RetryCount);
            }

            await repo.UpdateOutboxEventAsync(outbox, ct);
        }
    }
}

/// <summary>Interface สำหรับ OMS Callback HTTP call — implement ใน Infrastructure</summary>
public interface IOmsCallbackSender
{
    Task<bool> SendAsync(string omsProviderCode, string payload, CancellationToken ct = default);
}
