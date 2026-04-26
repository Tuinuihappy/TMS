using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.ValueObjects;
using static Tms.Orders.Domain.Enums.WeightUnit;
using Tms.SharedKernel.Exceptions;
using Xunit;

namespace Tms.Orders.UnitTests.Domain;

public sealed class TransportOrderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Address BkkPickup() =>
        Address.Create("123 ถ.สุขุมวิท", "คลองเตย", "คลองเตย", "กรุงเทพมหานคร", "10110");

    private static Address BkkDropoff() =>
        Address.Create("456 ถ.พหลโยธิน", "จตุจักร", "จตุจักร", "กรุงเทพมหานคร", "10900");

    private static TransportOrder CreateDraftOrder(string? notes = null)
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), notes: notes, tenantId: Guid.NewGuid());
        var stop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        order.AddStop(stop);
        return order;
    }

    private static (TransportOrder order, OrderStop stop) CreateDraftOrderWithStop(string? notes = null)
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), notes: notes, tenantId: Guid.NewGuid());
        var stop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        order.AddStop(stop);
        return (order, stop);
    }

    private static TransportOrder CreateConfirmedOrder()
    {
        var (order, stop) = CreateDraftOrderWithStop();
        var item = OrderItem.Create(stop.Id, "Item", 50m, 0.2m, 1);
        stop.AddItem(item);
        order.AddStop(stop);   // stop already added — but weight is now in stop before AddStop
        // Rebuild: clear and redo properly
        var order2 = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        var stop2 = OrderStop.Create(order2.Id, 1, BkkPickup(), BkkDropoff());
        stop2.AddItem(OrderItem.Create(stop2.Id, "Item", 50m, 0.2m, 1));
        order2.AddStop(stop2);
        order2.Confirm();
        return order2;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        var order = CreateDraftOrder();
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    // ── AddStop ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddStop_ShouldAccumulateTotalWeight()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());

        var stop1 = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        stop1.AddItem(OrderItem.Create(stop1.Id, "Item 1", 100m, 0.5m, 2));  // 200 kg
        order.AddStop(stop1);

        var stop2 = OrderStop.Create(order.Id, 2,
            Address.Create("1 ถ.ลาดพร้าว", "ลาดพร้าว", "ลาดพร้าว", "กรุงเทพมหานคร", "10230"),
            BkkDropoff());
        stop2.AddItem(OrderItem.Create(stop2.Id, "Item 2", 50m, 0.3m, 1));   // 50 kg
        order.AddStop(stop2);

        Assert.Equal(250m, order.TotalWeight);
        Assert.Equal(2, order.Stops.Count);
    }

    [Fact]
    public void AddStop_ToConfirmedOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        var extraStop = OrderStop.Create(order.Id, 2, BkkPickup(), BkkDropoff());

        Assert.Throws<DomainException>(() => order.AddStop(extraStop));
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_WithStopAndItems_ShouldChangeStatusToConfirmed()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        var stop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        stop.AddItem(OrderItem.Create(stop.Id, "สินค้าทดสอบ", 100m, 0.5m, 2));
        order.AddStop(stop);

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WithNoStops_ShouldThrowDomainException()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        Assert.Throws<DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Confirm_WithStopButNoItems_ShouldThrowDomainException()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        var emptyStop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        order.AddStop(emptyStop);

        Assert.Throws<DomainException>(() => order.Confirm());
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        var order = CreateDraftOrder();
        order.Cancel("ยกเลิกโดยลูกค้า");
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_ShouldSetCancelReason()
    {
        var order = CreateDraftOrder();
        order.Cancel("ยกเลิกโดยลูกค้า");
        Assert.Equal("ยกเลิกโดยลูกค้า", order.CancelReason);
    }

    [Fact]
    public void Cancel_ShouldNotMutateNotes()
    {
        const string originalNotes = "หมายเหตุต้นฉบับ";
        var order = CreateDraftOrder(notes: originalNotes);
        order.Cancel("ยกเลิก");
        Assert.Equal(originalNotes, order.Notes);
    }

    [Fact]
    public void Cancel_OnCompletedOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.MarkAsInTransit();
        order.Complete();
        Assert.Throws<DomainException>(() => order.Cancel("ลองยกเลิก"));
    }

    [Fact]
    public void Cancel_OnPlannedOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        Assert.Throws<DomainException>(() => order.Cancel("ลองยกเลิก"));
    }

    [Fact]
    public void Cancel_OnInTransitOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.MarkAsInTransit();
        Assert.Throws<DomainException>(() => order.Cancel("ลองยกเลิก"));
    }

    // ── TotalWeight / TotalVolumeCBM ──────────────────────────────────────────

    [Fact]
    public void TotalWeight_ShouldSumAllItemsAcrossStops()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());

        var stop1 = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        stop1.AddItem(OrderItem.Create(stop1.Id, "Item 1", 100m, 0.5m, 2));  // 200 kg
        stop1.AddItem(OrderItem.Create(stop1.Id, "Item 2", 50m, 0.3m, 1));   // 50 kg
        order.AddStop(stop1);

        Assert.Equal(250m, order.TotalWeight);
    }

    [Fact]
    public void TotalWeight_WithTonUnit_ShouldConvertToKg()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        var stop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        stop.AddItem(OrderItem.Create(stop.Id, "Heavy cargo", 2m, 1m, 1, weightUnit: Ton));
        order.AddStop(stop);

        Assert.Equal(2000m, order.TotalWeight);
    }

    [Fact]
    public void TotalVolumeCBM_ShouldSumAllItemsAcrossStops()
    {
        var order = TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), tenantId: Guid.NewGuid());
        var stop = OrderStop.Create(order.Id, 1, BkkPickup(), BkkDropoff());
        stop.AddItem(OrderItem.Create(stop.Id, "Item 1", 100m, 0.5m, 2));  // 1.0 CBM
        stop.AddItem(OrderItem.Create(stop.Id, "Item 2", 50m, 0.3m, 1));   // 0.3 CBM
        order.AddStop(stop);

        Assert.Equal(1.3m, order.TotalVolumeCBM);
    }

    // ── State Machine ─────────────────────────────────────────────────────────

    [Fact]
    public void MarkAsPlanned_FromConfirmed_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        Assert.Equal(OrderStatus.Planned, order.Status);
    }

    [Fact]
    public void MarkAsPlanned_FromDraft_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
        Assert.Throws<DomainException>(() => order.MarkAsPlanned());
    }

    [Fact]
    public void MarkAsInTransit_FromPlanned_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.MarkAsInTransit();
        Assert.Equal(OrderStatus.InTransit, order.Status);
    }

    [Fact]
    public void Complete_FromInTransit_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.MarkAsInTransit();
        order.Complete();
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Complete_FromPlanned_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        Assert.Throws<DomainException>(() => order.Complete());
    }

    // ── RevertToConfirmed ─────────────────────────────────────────────────────

    [Fact]
    public void RevertToConfirmed_FromPlanned_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.RevertToConfirmed();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void RevertToConfirmed_FromInTransit_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        order.MarkAsInTransit();
        order.RevertToConfirmed();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void RevertToConfirmed_FromDraft_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
        Assert.Throws<DomainException>(() => order.RevertToConfirmed());
    }

    // ── AmendStop ─────────────────────────────────────────────────────────────

    [Fact]
    public void AmendStop_OnDraftOrder_ShouldUpdatePickupAddress()
    {
        var (order, stop) = CreateDraftOrderWithStop();
        var newPickup = Address.Create("789 ถ.รัชดา", "ห้วยขวาง", "ห้วยขวาง", "กรุงเทพมหานคร", "10310");

        order.AmendStop(stop.Id, newPickup: newPickup);

        Assert.Equal("789 ถ.รัชดา", order.Stops.First().PickupAddress.Street);
    }

    [Fact]
    public void AmendStop_OnConfirmedOrder_ShouldSucceedAndRevertToDraft()
    {
        var order = CreateConfirmedOrder();
        var stopId = order.Stops.First().Id;
        var newDropoff = Address.Create("1 ถ.สีลม", "สีลม", "บางรัก", "กรุงเทพมหานคร", "10500");

        order.AmendStop(stopId, newDropoff: newDropoff);

        Assert.Equal("1 ถ.สีลม", order.Stops.First().DropoffAddress.Street);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void AmendStop_OnPlannedOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        var stopId = order.Stops.First().Id;
        var newPickup = Address.Create("X", "X", "X", "X", "00000");

        Assert.Throws<DomainException>(() => order.AmendStop(stopId, newPickup: newPickup));
    }

    [Fact]
    public void AmendStop_WithUnknownStopId_ShouldThrowNotFoundException()
    {
        var (order, _) = CreateDraftOrderWithStop();
        var newPickup = Address.Create("X", "X", "X", "X", "00000");

        Assert.Throws<NotFoundException>(() => order.AmendStop(Guid.NewGuid(), newPickup: newPickup));
    }

    // ── AmendDetails ──────────────────────────────────────────────────────────

    [Fact]
    public void AmendDetails_ShouldUpdatePriority()
    {
        var order = CreateDraftOrder();
        order.AmendDetails(newPriority: OrderPriority.Express);
        Assert.Equal(OrderPriority.Express, order.Priority);
    }

    [Fact]
    public void AmendDetails_OnConfirmedOrder_ShouldRevertToDraft()
    {
        var order = CreateConfirmedOrder();
        order.AmendDetails(newNotes: "Updated");
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    // ── Dangerous Goods ───────────────────────────────────────────────────────

    [Fact]
    public void OrderItem_DangerousGoods_WithDGClass_ShouldSucceed()
    {
        var (order, stop) = CreateDraftOrderWithStop();
        var item = OrderItem.Create(stop.Id, "สารเคมี", 20m, 0.1m, 1,
            isDangerousGoods: true, unNumber: "UN1234", dgClass: "3");

        Assert.True(item.IsDangerousGoods);
        Assert.Equal("3", item.DGClass);
    }

    [Fact]
    public void OrderItem_DangerousGoods_WithoutDGClass_ShouldThrowArgumentException()
    {
        var (_, stop) = CreateDraftOrderWithStop();
        Assert.Throws<ArgumentException>(() =>
            OrderItem.Create(stop.Id, "สารเคมี", 20m, 0.1m, 1,
                isDangerousGoods: true, dgClass: null));
    }
}
