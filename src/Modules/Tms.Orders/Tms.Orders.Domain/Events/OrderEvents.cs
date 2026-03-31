using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.Events;

public sealed record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record OrderConfirmedEvent(
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record OrderCancelledEvent(
    Guid OrderId,
    string OrderNumber,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record OrderAmendedEvent(
    Guid OrderId,
    string OrderNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
