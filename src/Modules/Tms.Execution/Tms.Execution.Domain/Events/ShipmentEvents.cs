using Tms.SharedKernel.Domain;

namespace Tms.Execution.Domain.Events;

public sealed record ShipmentPickedUpEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid TripId,
    Guid OrderId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record ShipmentDeliveredEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid TripId,
    Guid OrderId,
    DateTime DeliveredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record ShipmentExceptionEvent(
    Guid ShipmentId,
    string ShipmentNumber,
    Guid OrderId,
    string ExceptionType,
    string ReasonCode,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
