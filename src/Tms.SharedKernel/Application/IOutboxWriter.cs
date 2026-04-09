using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Infrastructure.Outbox;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Writes integration events as OutboxMessage rows into the CURRENT module's DbContext
/// within the same transaction as the aggregate save.
/// Each module registers its own keyed implementation backed by its own DbContext.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Stages an integration event as an OutboxMessage (not yet saved — SaveChanges is called by the repo).</summary>
    void Stage(IIntegrationEvent @event);

    /// <summary>Stages multiple events atomically.</summary>
    void StageRange(IEnumerable<IIntegrationEvent> events);
}

/// <summary>
/// Generic implementation — each module injects its own <typeparamref name="TContext"/>.
/// </summary>
public sealed class OutboxWriter<TContext>(TContext dbContext) : IOutboxWriter
    where TContext : DbContext
{
    public void Stage(IIntegrationEvent @event)
    {
        var message = new OutboxMessage
        {
            Type    = @event.GetType().AssemblyQualifiedName ?? @event.GetType().Name,
            Content = JsonSerializer.Serialize(@event, @event.GetType())
        };
        dbContext.Set<OutboxMessage>().Add(message);
    }

    public void StageRange(IEnumerable<IIntegrationEvent> events)
    {
        foreach (var @event in events)
            Stage(@event);
    }
}
