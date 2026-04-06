using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Integration.Application.Acl;
using Tms.Integration.Domain.Interfaces;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.Features.OmsIntegration.ProcessSync;

/// <summary>
/// Background Worker หยิบ OmsOrderSync ที่ Status=Pending ทุก 5 วินาที
/// แปลงผ่าน ACL แล้วเรียก CreateOrderCommand เพื่อสร้าง Order ใน Tms.Orders.
/// </summary>
public sealed class ProcessOmsSyncWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessOmsSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OMS Sync Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OMS Sync Worker encountered an unhandled error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOmsSyncRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var mapper = scope.ServiceProvider.GetRequiredService<OmsAclMapper>();
        var publisher = scope.ServiceProvider.GetRequiredService<Tms.SharedKernel.Application.IIntegrationEventPublisher>();

        // รวม Pending + Failed-ready-for-retry
        var pendingSyncs = await repo.GetPendingAsync(batchSize: 20, ct);
        var retryableSyncs = await repo.GetFailedForRetryAsync(ct);
        var allSyncs = pendingSyncs.Concat(retryableSyncs).ToList();

        foreach (var sync in allSyncs)
        {
            try
            {
                // ดึง Field Mapping ของ Provider นี้
                var mappings = await repo.GetMappingsAsync(sync.OmsProviderCode, ct);

                // ACL: แปลง raw → CreateOrderCommand
                var mappedJson = sync.RawPayload; // ใช้ raw ส่งตรงใน MVP
                var command = mapper.Map(sync.RawPayload, mappings);

                sync.MarkProcessing(mappedJson);
                await repo.UpdateAsync(sync, ct);

                // เรียก Tms.Orders ผ่าน MediatR (In-Process)
                var orderId = await sender.Send(command, ct);

                sync.MarkSucceeded(orderId);
                await repo.UpdateAsync(sync, ct);

                // Publish event ให้ Module อื่น (Planning, Notification) รับรู้
                await publisher.PublishAsync(new OrderSyncedFromOmsIntegrationEvent(
                    SyncId: sync.Id,
                    ExternalOrderRef: sync.ExternalOrderRef,
                    OmsProviderCode: sync.OmsProviderCode,
                    TmsOrderId: orderId,
                    OrderNumber: $"ORD-OMS-{sync.ExternalOrderRef[..Math.Min(8, sync.ExternalOrderRef.Length)]}"
                ), ct);

                logger.LogInformation("OMS sync {SyncId} succeeded. TMS Order: {OrderId}", sync.Id, orderId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OMS sync {SyncId} failed. Retry {Retry}", sync.Id, sync.RetryCount + 1);
                sync.MarkFailed(ex.Message);
                await repo.UpdateAsync(sync, ct);
            }
        }
    }
}
