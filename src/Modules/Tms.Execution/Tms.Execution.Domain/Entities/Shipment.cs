using Tms.Execution.Domain.Enums;
using Tms.Execution.Domain.Events;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Domain.Entities;

public sealed class Shipment : AggregateRoot
{
    public string ShipmentNumber { get; private set; } = string.Empty;
    public Guid TripId { get; private set; }
    public Guid OrderId { get; private set; }
    /// <summary>FK ไปยัง Stop (Dropoff) — 1 Shipment ต่อ Order ต่อ Trip</summary>
    public Guid DropoffStopId { get; private set; }
    public Guid TenantId { get; private set; }
    public ShipmentStatus Status { get; private set; }

    // Address snapshot
    public string? AddressName { get; private set; }
    public string? AddressStreet { get; private set; }
    public string? AddressProvince { get; private set; }
    public double? AddressLatitude { get; private set; }
    public double? AddressLongitude { get; private set; }
    /// <summary>FK to Master Data Location — used by Geofence auto-arrive at DROPOFF</summary>
    public Guid? DestinationLocationId { get; private set; }
    /// <summary>FK to Master Data Location — used by Geofence auto-arrive at PICKUP</summary>
    public Guid? PickupLocationId { get; private set; }

    // Exception
    public string? ExceptionReason { get; private set; }
    public string? ExceptionReasonCode { get; private set; }

    // Timestamps
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<ShipmentItem> _items = [];
    public IReadOnlyCollection<ShipmentItem> Items => _items.AsReadOnly();

    public PODRecord? POD { get; private set; }

    private Shipment() { } // EF Core

    public static Shipment Create(
        string shipmentNumber,
        Guid tripId,
        Guid orderId,
        Guid dropoffStopId,
        Guid tenantId,
        string? addressName = null,
        string? addressStreet = null,
        string? addressProvince = null,
        double? lat = null,
        double? lng = null,
        Guid? destinationLocationId = null,
        Guid? pickupLocationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shipmentNumber);

        return new Shipment
        {
            ShipmentNumber = shipmentNumber,
            TripId = tripId,
            OrderId = orderId,
            DropoffStopId = dropoffStopId,
            TenantId = tenantId,
            Status = ShipmentStatus.Pending,
            AddressName = addressName,
            AddressStreet = addressStreet,
            AddressProvince = addressProvince,
            AddressLatitude = lat,
            AddressLongitude = lng,
            DestinationLocationId = destinationLocationId,
            PickupLocationId      = pickupLocationId,
            CreatedAt             = DateTime.UtcNow
        };
    }


    public void AddItem(ShipmentItem item)
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException("Items can only be added when Shipment is Pending.", "SHIPMENT_NOT_PENDING");
        _items.Add(item);
    }

    // ──── State Transitions ────────────────────────────────────────────

    /// <summary>Rule 1: PickUp ได้เฉพาะ Status = Pending</summary>
    public void PickUp()
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException(
                $"Cannot pick up shipment with status '{Status}'.",
                "INVALID_SHIPMENT_STATE");

        Status = ShipmentStatus.PickedUp;
        PickedUpAt = DateTime.UtcNow;
        AddDomainEvent(new ShipmentPickedUpEvent(Id, ShipmentNumber, TripId, OrderId));
    }

    public void StartTransit()
    {
        if (Status != ShipmentStatus.PickedUp)
            throw new DomainException(
                $"Cannot start transit from status '{Status}'.",
                "INVALID_SHIPMENT_STATE");
        Status = ShipmentStatus.InTransit;
    }

    public void Arrive()
    {
        if (Status is not (ShipmentStatus.PickedUp or ShipmentStatus.InTransit))
            throw new DomainException(
                $"Cannot arrive from status '{Status}'.",
                "INVALID_SHIPMENT_STATE");
        Status = ShipmentStatus.Arrived;
        ArrivedAt = DateTime.UtcNow;
    }

    /// <summary>Rule 2+3: Deliver ต้องมี POD และ DeliveredQty > 0</summary>
    public void Deliver(
        IEnumerable<(Guid itemId, int deliveredQty)> deliveredItems,
        PODRecord pod)
    {
        if (Status != ShipmentStatus.Arrived)
            throw new DomainException(
                $"Cannot deliver from status '{Status}'.",
                "INVALID_SHIPMENT_STATE");

        ArgumentNullException.ThrowIfNull(pod, "POD is required for delivery.");

        var deliveredList = deliveredItems.ToList();
        if (deliveredList.Sum(d => d.deliveredQty) == 0)
            throw new DomainException(
                "At least one item must have delivered quantity > 0.",
                "NO_ITEMS_DELIVERED");

        ApplyDeliveredItems(deliveredList);
        POD = pod;
        Status = ShipmentStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentDeliveredEvent(Id, ShipmentNumber, TripId, OrderId, DeliveredAt!.Value));
    }

    /// <summary>Rule 4: PartialDeliver — ส่งได้บางส่วน</summary>
    public void PartialDeliver(
        IEnumerable<(Guid itemId, int deliveredQty)> deliveredItems,
        PODRecord pod)
    {
        if (Status != ShipmentStatus.Arrived)
            throw new DomainException(
                $"Cannot partial-deliver from status '{Status}'.",
                "INVALID_SHIPMENT_STATE");

        ArgumentNullException.ThrowIfNull(pod, "POD is required for partial delivery.");

        var deliveredList = deliveredItems.ToList();
        if (deliveredList.Sum(d => d.deliveredQty) == 0)
            throw new DomainException(
                "At least one item must have delivered quantity > 0.",
                "NO_ITEMS_DELIVERED");

        ApplyDeliveredItems(deliveredList);
        POD = pod;
        Status = ShipmentStatus.PartiallyDelivered;
        DeliveredAt = DateTime.UtcNow;

        AddDomainEvent(new ShipmentDeliveredEvent(Id, ShipmentNumber, TripId, OrderId, DeliveredAt!.Value));
    }

    public void Reject(string reason, string reasonCode)
    {
        if (Status != ShipmentStatus.Arrived)
            throw new DomainException(
                $"Cannot reject from status '{Status}'.",
                "INVALID_SHIPMENT_STATE");

        foreach (var item in _items) item.SetReturned();
        ExceptionReason = reason;
        ExceptionReasonCode = reasonCode;
        Status = ShipmentStatus.Returned;

        AddDomainEvent(new ShipmentExceptionEvent(Id, ShipmentNumber, OrderId, "Rejected", reasonCode, reason));
    }

    /// <summary>Rule 5: Exception reason ต้องอ้าง ReasonCode</summary>
    public void RecordException(string reason, string reasonCode)
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Returned)
            throw new DomainException(
                $"Cannot record exception on status '{Status}'.",
                "INVALID_SHIPMENT_STATE");

        ExceptionReason = reason;
        ExceptionReasonCode = reasonCode;
        Status = ShipmentStatus.Exception;

        AddDomainEvent(new ShipmentExceptionEvent(Id, ShipmentNumber, OrderId, "Exception", reasonCode, reason));
    }

    public void ApprovePOD(Guid approvedBy)
    {
        if (POD is null)
            throw new DomainException("No POD record exists for this shipment.", "NO_POD");
        POD.Approve(approvedBy);
    }

    // ──── Private Helpers ──────────────────────────────────────────────

    private void ApplyDeliveredItems(IEnumerable<(Guid itemId, int deliveredQty)> deliveredItems)
    {
        foreach (var (itemId, qty) in deliveredItems)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new NotFoundException(nameof(ShipmentItem), itemId);
            item.SetDelivered(qty);
        }
    }
}
