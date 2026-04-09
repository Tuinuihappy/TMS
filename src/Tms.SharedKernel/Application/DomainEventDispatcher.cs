using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Infrastructure.Outbox;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Converts domain events from aggregates into OutboxMessage rows within the SAME transaction.
/// Called by each module's DbContext.SaveChangesAsync override.
/// </summary>
public static class DomainEventDispatcher
{
    public static void StoreDomainEventsInOutbox(DbContext context)
    {
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear domain events before persisting to avoid re-entrancy
        foreach (var entity in entities)
            entity.ClearDomainEvents();

        if (domainEvents.Count == 0) return;

        // Add OutboxMessage rows to the SAME DbContext — saved atomically with the aggregate
        foreach (var domainEvent in domainEvents)
        {
            var message = new OutboxMessage
            {
                Type = domainEvent.GetType().AssemblyQualifiedName
                       ?? domainEvent.GetType().Name,
                Content = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
            };
            context.Set<OutboxMessage>().Add(message);
        }
    }
}

