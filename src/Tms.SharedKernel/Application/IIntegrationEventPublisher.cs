using MediatR;

namespace Tms.SharedKernel.Application;

/// <summary>Publishes integration events to all subscribers in-process</summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default);
    Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default);
}

/// <summary>In-process implementation using MediatR INotification</summary>
public sealed class MediatRIntegrationEventPublisher(IPublisher publisher) : IIntegrationEventPublisher
{
    public Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default) =>
        publisher.Publish(@event, ct);

    public async Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
            await publisher.Publish(@event, ct);
    }
}
