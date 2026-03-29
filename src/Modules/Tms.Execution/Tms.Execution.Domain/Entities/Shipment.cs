using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Domain.Entities;

public enum ShipmentStatus
{
    Pending = 0, PickedUp = 1, InTransit = 2,
    Arrived = 3, Delivered = 4, PartiallyDelivered = 5, Returned = 6
}

public sealed class Shipment : AggregateRoot
{
    public Guid TripId { get; private set; }
    public Guid OrderId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? Notes { get; private set; }

    private Shipment() { }

    public static Shipment Create(Guid tripId, Guid orderId)
    {
        return new Shipment
        {
            TripId = tripId,
            OrderId = orderId,
            Status = ShipmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void PickUp()
    {
        if (Status != ShipmentStatus.Pending)
            throw new DomainException("Only Pending shipments can be picked up.");
        Status = ShipmentStatus.PickedUp;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deliver(string? notes = null)
    {
        if (Status is not (ShipmentStatus.PickedUp or ShipmentStatus.InTransit))
            throw new DomainException("Cannot deliver shipment in current status.");
        Status = ShipmentStatus.Delivered;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
