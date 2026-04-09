using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.Documents.Infrastructure.Persistence;
using Tms.Execution.Infrastructure.Persistence;
using Tms.Integration.Infrastructure.Persistence;
using Tms.Orders.Infrastructure.Persistence;
using Tms.Planning.Infrastructure.Persistence;
using Tms.Platform.Infrastructure.Persistence;
using Tms.Resources.Infrastructure.Persistence;
using Tms.SharedKernel.Infrastructure.Outbox;
using Tms.Tracking.Infrastructure.Persistence;

namespace Tms.WebApi.Infrastructure.BackgroundJobs;

/// <summary>
/// Polls every module's OutboxMessages table and dispatches unprocessed events via MediatR.
/// Implements exponential-backoff retry (30s × 2^n) and dead-letter after MaxRetries.
/// </summary>
public sealed class OutboxProcessorJob(
    IServiceProvider serviceProvider,
    ILogger<OutboxProcessorJob> logger) : BackgroundService
{
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessAllModulesAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Unhandled error in OutboxProcessorJob."); }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessAllModulesAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var publisher = sp.GetRequiredService<IPublisher>();

        // Each module's DbContext is processed independently
        var contexts = new (string Name, DbContext Ctx)[]
        {
            ("Orders",      sp.GetRequiredService<OrdersDbContext>()),
            ("Planning",    sp.GetRequiredService<PlanningDbContext>()),
            ("Execution",   sp.GetRequiredService<ExecutionDbContext>()),
            ("Tracking",    sp.GetRequiredService<TrackingDbContext>()),
            ("Platform",    sp.GetRequiredService<PlatformDbContext>()),
            ("Resources",   sp.GetRequiredService<ResourcesDbContext>()),
            ("Documents",   sp.GetRequiredService<DocumentsDbContext>()),
            ("Integration", sp.GetRequiredService<IntegrationDbContext>()),
        };

        foreach (var (name, ctx) in contexts)
        {
            try { await ProcessContextAsync(name, ctx, publisher, ct); }
            catch (Exception ex) { logger.LogError(ex, "Error processing outbox for module {Module}.", name); }
        }
    }

    private async Task ProcessContextAsync(
        string moduleName, DbContext ctx, IPublisher publisher, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Fetch eligible messages: not processed, not dead-letter, not waiting for backoff
        var messages = await ctx.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null
                     && !m.IsDeadLetter
                     && (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.OccurredOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var type = Type.GetType(message.Type);
                if (type is null)
                    throw new InvalidOperationException($"Cannot resolve type '{message.Type}'");

                var domainEvent = JsonSerializer.Deserialize(message.Content, type)
                    ?? throw new InvalidOperationException($"Deserialized null for '{message.Type}'");

                if (domainEvent is not INotification notification)
                    throw new InvalidOperationException($"'{message.Type}' is not INotification");

                await publisher.Publish(notification, ct);

                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
                logger.LogInformation("[Outbox:{Module}] Processed {Id} ({Type})", moduleName, message.Id, message.Type);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[Outbox:{Module}] Failed to process {Id} (retry {N}). Scheduling retry.",
                    moduleName, message.Id, message.RetryCount + 1);

                message.ScheduleRetry(ex.ToString()[..Math.Min(2000, ex.ToString().Length)]);

                if (message.IsDeadLetter)
                    logger.LogError("[Outbox:{Module}] Message {Id} moved to DLQ after {N} retries.",
                        moduleName, message.Id, OutboxMessage.MaxRetries);
            }
        }

        await ctx.SaveChangesAsync(ct);
    }
}

