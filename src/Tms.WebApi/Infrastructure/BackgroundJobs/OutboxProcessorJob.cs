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

public sealed class OutboxProcessorJob(
    IServiceProvider serviceProvider, 
    ILogger<OutboxProcessorJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // We process outboxes for all modules
        var contexts = new DbContext[]
        {
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>(),
            scope.ServiceProvider.GetRequiredService<ExecutionDbContext>(),
            scope.ServiceProvider.GetRequiredService<PlanningDbContext>(),
            scope.ServiceProvider.GetRequiredService<TrackingDbContext>(),
            scope.ServiceProvider.GetRequiredService<PlatformDbContext>(),
            scope.ServiceProvider.GetRequiredService<ResourcesDbContext>(),
            scope.ServiceProvider.GetRequiredService<DocumentsDbContext>(),
            scope.ServiceProvider.GetRequiredService<IntegrationDbContext>()
        };

        foreach (var context in contexts)
        {
            var messages = await context.Set<OutboxMessage>()
                .Where(m => m.ProcessedOn == null)
                .OrderBy(m => m.OccurredOn)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                logger.LogInformation("Processing OutboxMessage {Id} ({Type})", message.Id, message.Type);

                try
                {
                    var type = Type.GetType(message.Type);
                    if (type is null)
                    {
                        throw new InvalidOperationException($"Could not resolve type {message.Type}");
                    }

                    var domainEvent = JsonSerializer.Deserialize(message.Content, type);
                    if (domainEvent is INotification notification)
                    {
                        await publisher.Publish(notification, cancellationToken);
                        message.ProcessedOn = DateTime.UtcNow;
                    }
                    else
                    {
                        throw new InvalidOperationException("Failed to cast domain event to INotification.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                    message.Error = ex.ToString();
                }
            }

            if (messages.Any())
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
