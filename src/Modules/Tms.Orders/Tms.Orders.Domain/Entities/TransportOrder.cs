using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Events;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Domain.Entities;

public sealed class TransportOrder : AggregateRoot
{
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public OrderPriority Priority { get; private set; }
    public Guid TenantId { get; private set; }
    public string? Notes { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private readonly List<OrderStop> _stops = [];
    public IReadOnlyCollection<OrderStop> Stops => _stops.AsReadOnly();

    // Stored columns — updated by AddStop/AmendStop to avoid SQL subqueries on every read
    public decimal TotalWeight { get; private set; }
    public decimal TotalVolumeCBM { get; private set; }

    private TransportOrder() { }  // EF Core

    // ── Factories ─────────────────────────────────────────────────────────────

    public static TransportOrder Create(
        string orderNumber,
        Guid customerId,
        OrderPriority priority = OrderPriority.Normal,
        string? notes = null,
        Guid tenantId = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        var order = new TransportOrder
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            TenantId = tenantId,
            Status = OrderStatus.Draft,
            Priority = priority,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.OrderNumber, order.CustomerId));
        return order;
    }

    public static TransportOrder CreateWithUser(
        string orderNumber,
        Guid customerId,
        Guid createdBy,
        OrderPriority priority = OrderPriority.Normal,
        string? notes = null,
        Guid tenantId = default)
    {
        var order = Create(orderNumber, customerId, priority, notes, tenantId);
        order.CreatedBy = createdBy;
        return order;
    }

    // ── Stop Mutations ────────────────────────────────────────────────────────

    public void AddStop(OrderStop stop)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Stops can only be added to Draft orders.", "ORDER_NOT_DRAFT");

        _stops.Add(stop);
        TotalWeight += stop.StopTotalWeight;
        TotalVolumeCBM += stop.StopTotalVolumeCBM;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AmendStop(
        Guid stopId,
        Address? newPickup = null,
        Address? newDropoff = null,
        TimeWindow? newPickupWindow = null,
        TimeWindow? newDropoffWindow = null)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
            throw new DomainException(
                $"Cannot amend stop on order with status '{Status}'. Only Draft or Confirmed orders can be amended.",
                "ORDER_CANNOT_AMEND");

        var stop = _stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new NotFoundException(nameof(OrderStop), stopId);

        stop.Amend(newPickup, newDropoff, newPickupWindow, newDropoffWindow);

        // Recompute stored totals from all stops
        TotalWeight = _stops.Sum(s => s.StopTotalWeight);
        TotalVolumeCBM = _stops.Sum(s => s.StopTotalVolumeCBM);

        if (Status == OrderStatus.Confirmed)
            Status = OrderStatus.Draft;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAmendedEvent(Id, OrderNumber));
    }

    // ── Order-level Mutations ─────────────────────────────────────────────────

    public void AmendDetails(string? newNotes = null, OrderPriority? newPriority = null)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
            throw new DomainException(
                $"Cannot amend order with status '{Status}'. Only Draft or Confirmed orders can be amended.",
                "ORDER_CANNOT_AMEND");

        if (newNotes is not null) Notes = newNotes;
        if (newPriority.HasValue) Priority = newPriority.Value;

        if (Status == OrderStatus.Confirmed)
            Status = OrderStatus.Draft;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAmendedEvent(Id, OrderNumber));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only Draft orders can be confirmed.", "ORDER_CANNOT_CONFIRM");
        if (!_stops.Any() || _stops.All(s => !s.Items.Any()))
            throw new DomainException("Order must have at least one stop with at least one item.", "ORDER_NO_ITEMS");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, CustomerId));
    }

    public void Cancel(string reason)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
            throw new DomainException($"Cannot cancel an order with status '{Status}'. Only Draft or Confirmed orders can be cancelled.", "ORDER_CANNOT_CANCEL");

        Status = OrderStatus.Cancelled;
        CancelReason = reason;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, reason));
    }

    public void RevertToConfirmed()
    {
        if (Status is not (OrderStatus.Planned or OrderStatus.InTransit))
            throw new DomainException(
                $"Cannot revert order with status '{Status}' to Confirmed.",
                "ORDER_CANNOT_REVERT");

        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsPlanned()
    {
        if (Status != OrderStatus.Confirmed)
            throw new DomainException("Only Confirmed orders can be marked as Planned.", "ORDER_CANNOT_PLAN");

        Status = OrderStatus.Planned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsInTransit()
    {
        if (Status != OrderStatus.Planned)
            throw new DomainException("Only Planned orders can go In-Transit.", "ORDER_CANNOT_TRANSIT");

        Status = OrderStatus.InTransit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != OrderStatus.InTransit)
            throw new DomainException("Only In-Transit orders can be completed.", "ORDER_CANNOT_COMPLETE");

        Status = OrderStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
