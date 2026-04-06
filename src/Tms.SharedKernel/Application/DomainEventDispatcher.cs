using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Domain;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Dispatches domain events after SaveChanges.
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

        // Clear before dispatching
        foreach (var entity in entities)
            entity.ClearDomainEvents();

        if (domainEvents.Count == 0) return;

        var outboxMessages = domainEvents.Select(domainEvent => new Tms.SharedKernel.Infrastructure.Outbox.OutboxMessage
        {
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().Name,
            Content = System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        }).ToList();

    }
}
