using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Domain.Entities;

/// <summary>
/// Physical Movement Unit — ขนสินค้าจาก PickupAddress → DropoffAddress
/// TransportOrder หนึ่งใบมีหลาย OrderStop ได้ แต่ละ stop มี address และ items ของตัวเอง
/// </summary>
public sealed class OrderStop : BaseEntity
{
    public Guid OrderId { get; private set; }
    public int Sequence { get; private set; }
    public Address PickupAddress { get; private set; } = null!;
    public Address DropoffAddress { get; private set; } = null!;
    public TimeWindow? PickupWindow { get; private set; }
    public TimeWindow? DropoffWindow { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Computed — ใช้โดย TransportOrder เพื่อ recompute stored totals; ไม่ persist
    public decimal StopTotalWeight => _items.Sum(i => i.Weight.ToKg() * i.Quantity);
    public decimal StopTotalVolumeCBM => _items.Sum(i => i.VolumeCBM * i.Quantity);

    private OrderStop() { }  // EF Core

    public static OrderStop Create(
        Guid orderId,
        int sequence,
        Address pickupAddress,
        Address dropoffAddress,
        TimeWindow? pickupWindow = null,
        TimeWindow? dropoffWindow = null)
    {
        if (pickupWindow is not null && dropoffWindow is not null &&
            dropoffWindow.From <= pickupWindow.From)
            throw new DomainException(
                "Dropoff window must start after pickup window.", "INVALID_TIME_WINDOWS");

        return new OrderStop
        {
            OrderId = orderId,
            Sequence = sequence,
            PickupAddress = pickupAddress,
            DropoffAddress = dropoffAddress,
            PickupWindow = pickupWindow,
            DropoffWindow = dropoffWindow,
        };
    }

    public void AddItem(OrderItem item) => _items.Add(item);

    internal void Amend(
        Address? newPickup = null,
        Address? newDropoff = null,
        TimeWindow? newPickupWindow = null,
        TimeWindow? newDropoffWindow = null)
    {
        if (newPickup is not null) PickupAddress = newPickup;
        if (newDropoff is not null) DropoffAddress = newDropoff;
        if (newPickupWindow is not null) PickupWindow = newPickupWindow;
        if (newDropoffWindow is not null) DropoffWindow = newDropoffWindow;
    }
}
