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

/// <summary>
/// เอนต์เมื่อ Order ถูก Split ออกเป็น child orders (Manual หรือ Auto)
/// </summary>
public sealed record OrderSplitEvent(
    Guid ParentOrderId,
    string ParentOrderNumber,
    List<Guid> ChildOrderIds,
    string SplitMode) : IDomainEvent   // "Manual" | "Auto"
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
