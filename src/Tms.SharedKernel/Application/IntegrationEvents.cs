using MediatR;

namespace Tms.SharedKernel.Application;

/// <summary>Marker interface for cross-module integration events (In-Process via MediatR)</summary>
public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

/// <summary>Base record for integration events — provides defaults</summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}
