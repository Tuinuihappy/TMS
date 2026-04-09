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
    public Address PickupAddress { get; private set; } = null!;
    public Address DropoffAddress { get; private set; } = null!;
    public TimeWindow? PickupWindow { get; private set; }
    public TimeWindow? DropoffWindow { get; private set; }
    public string? Notes { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    // ── Split Order Fields ────────────────────────────────────────────────────
    /// <summary>ชี้ไปยัง Order ต้นสาย — null ถ้าเป็น original order</summary>
    public Guid? ParentOrderId { get; private set; }
    /// <summary>เหตุผลที่ Split: "Manual" | "CapacityExceeded" | "RouteConstraint"</summary>
    public string? SplitReason { get; private set; }
    /// <summary>true ถ้าเป็น child order ที่ถูก split ออกมาจาก parent</summary>
    public bool IsSplitChild => ParentOrderId.HasValue;

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalWeight => _items.Sum(i => i.Weight * i.Quantity);
    public decimal TotalVolume => _items.Sum(i => i.Volume * i.Quantity);

    private TransportOrder() { }  // EF Core

    // ── Factories ─────────────────────────────────────────────────────────────

    public static TransportOrder Create(
        string orderNumber,
        Guid customerId,
        Address pickupAddress,
        Address dropoffAddress,
        OrderPriority priority = OrderPriority.Normal,
        TimeWindow? pickupWindow = null,
        TimeWindow? dropoffWindow = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        var order = new TransportOrder
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            Priority = priority,
            PickupAddress = pickupAddress,
            DropoffAddress = dropoffAddress,
            PickupWindow = pickupWindow,
            DropoffWindow = dropoffWindow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.OrderNumber, order.CustomerId));
        return order;
    }

    public static TransportOrder CreateWithUser(
        string orderNumber,
        Guid customerId,
        Address pickupAddress,
        Address dropoffAddress,
        Guid createdBy,
        OrderPriority priority = OrderPriority.Normal,
        TimeWindow? pickupWindow = null,
        TimeWindow? dropoffWindow = null,
        string? notes = null)
    {
        var order = Create(orderNumber, customerId, pickupAddress, dropoffAddress,
            priority, pickupWindow, dropoffWindow, notes);
        order.CreatedBy = createdBy;
        return order;
    }

    /// <summary>
    /// สร้าง child order จาก parent — inherit pickup/dropoff address จาก parent โดย default.
    /// Manual split สามารถ override dropoffAddress และ dropoffWindow ได้
    /// </summary>
    public static TransportOrder CreateSplitChild(
        string childOrderNumber,
        TransportOrder parent,
        string splitReason,
        Address? overrideDropoffAddress = null,
        TimeWindow? overrideDropoffWindow = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(childOrderNumber);
        ArgumentNullException.ThrowIfNull(parent);

        var child = new TransportOrder
        {
            OrderNumber = childOrderNumber,
            CustomerId = parent.CustomerId,
            Status = OrderStatus.Draft,
            Priority = parent.Priority,
            PickupAddress = parent.PickupAddress,
            DropoffAddress = overrideDropoffAddress ?? parent.DropoffAddress,
            PickupWindow = parent.PickupWindow,
            DropoffWindow = overrideDropoffWindow ?? parent.DropoffWindow,
            Notes = notes ?? $"Split from {parent.OrderNumber}",
            ParentOrderId = parent.Id,
            SplitReason = splitReason,
            CreatedBy = parent.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        child.AddDomainEvent(new OrderCreatedEvent(child.Id, child.OrderNumber, child.CustomerId));
        return child;
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Items can only be added to Draft orders.", "ORDER_NOT_DRAFT");

        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// เพิ่ม Item โดยไม่ตรวจ Status — ใช้สำหรับสร้าง Split Child orders
    /// (bypass Draft check ที่ AddItem ปกติมี)
    /// </summary>
    public void AddItemInternal(OrderItem item)
    {
        _items.Add(item);
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Only Draft orders can be confirmed.", "ORDER_CANNOT_CONFIRM");
        if (_items.Count == 0)
            throw new DomainException("Order must have at least one item.", "ORDER_NO_ITEMS");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, CustomerId));
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new DomainException($"Cannot cancel an order with status '{Status}'.", "ORDER_CANNOT_CANCEL");

        Status = OrderStatus.Cancelled;
        CancelReason = reason;
        Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes} | Cancel reason: {reason}";
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderCancelledEvent(Id, OrderNumber, reason));
    }

    /// <summary>
    /// Mark parent order ว่าถูก Split บางส่วนออกเป็น child orders.
    /// Status เปลี่ยนเป็น PartialSplit — ยัง Active แต่ plan ผ่าน child เท่านั้น
    /// </summary>
    public void MarkAsSplit(string splitReason, List<Guid> childOrderIds, string splitMode)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
            throw new DomainException(
                $"Cannot split order with status '{Status}'. Only Draft or Confirmed orders can be split.",
                "ORDER_CANNOT_SPLIT");

        if (childOrderIds.Count < 2)
            throw new DomainException("Split must produce at least 2 child orders.", "SPLIT_INSUFFICIENT_CHILDREN");

        SplitReason = splitReason;
        Status = OrderStatus.PartialSplit;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderSplitEvent(Id, OrderNumber, childOrderIds, splitMode));
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

    /// <summary>Amend Draft or Confirmed order — update pickup/dropoff/notes</summary>
    public void Amend(
        Address? newPickup = null,
        Address? newDropoff = null,
        TimeWindow? newPickupWindow = null,
        TimeWindow? newDropoffWindow = null,
        string? newNotes = null,
        OrderPriority? newPriority = null)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Confirmed))
            throw new DomainException(
                $"Cannot amend order with status '{Status}'. Only Draft or Confirmed orders can be amended.",
                "ORDER_CANNOT_AMEND");

        if (newPickup is not null) PickupAddress = newPickup;
        if (newDropoff is not null) DropoffAddress = newDropoff;
        if (newPickupWindow is not null) PickupWindow = newPickupWindow;
        if (newDropoffWindow is not null) DropoffWindow = newDropoffWindow;
        if (newNotes is not null) Notes = newNotes;
        if (newPriority.HasValue) Priority = newPriority.Value;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new OrderAmendedEvent(Id, OrderNumber));
    }
}
