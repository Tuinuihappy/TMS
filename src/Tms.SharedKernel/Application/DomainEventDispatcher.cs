using MediatR;
using Microsoft.EntityFrameworkCore;
using Tms.SharedKernel.Domain;

namespace Tms.SharedKernel.Application;

/// <summary>
/// Dispatches domain events after SaveChanges.
/// Call DispatchDomainEventsAsync in your DbContext.SaveChangesAsync override.
/// </summary>
public static class DomainEventDispatcher
{
    public static async Task DispatchDomainEventsAsync(DbContext context, IPublisher publisher, CancellationToken ct = default)
    {
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear before dispatching (avoid re-entry)
        foreach (var entity in entities)
            entity.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, ct);
    }
}
