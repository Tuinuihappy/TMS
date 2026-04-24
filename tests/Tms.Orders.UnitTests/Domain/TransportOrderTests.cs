using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.ValueObjects;
using static Tms.Orders.Domain.Enums.WeightUnit;
using Tms.SharedKernel.Exceptions;
using Xunit;

namespace Tms.Orders.UnitTests.Domain;

public sealed class TransportOrderTests
{
    private static TransportOrder CreateDraftOrder(string? notes = null)
    {
        var pickup = Address.Create("123 ถ.สุขุมวิท", "คลองเตย", "คลองเตย", "กรุงเทพมหานคร", "10110");
        var dropoff = Address.Create("456 ถ.พหลโยธิน", "จตุจักร", "จตุจักร", "กรุงเทพมหานคร", "10900");
        return TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), pickup, dropoff, notes: notes, tenantId: Guid.NewGuid());
    }

    private static TransportOrder CreateConfirmedOrder()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "Item", 50m, 0.2m, 1));
        order.Confirm();
        return order;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        var order = CreateDraftOrder();
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    // ── Confirm ───────────────────────────────────────────────────────────────

    [Fact]
    public void Confirm_WithItems_ShouldChangeStatusToConfirmed()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "สินค้าทดสอบ", 100m, 0.5m, 2));
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WithoutItems_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
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

    // ── AddItem ───────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ToConfirmedOrder_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "First item", 50m, 0.2m, 1));
        order.Confirm();

        Assert.Throws<DomainException>(() =>
            order.AddItem(OrderItem.Create(order.Id, "Second item", 30m, 0.1m, 1)));
    }

    // ── TotalWeight ───────────────────────────────────────────────────────────

    [Fact]
    public void TotalWeight_ShouldSumAllItemsInKg()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "Item 1", 100m, 0.5m, 2));  // 200kg
        order.AddItem(OrderItem.Create(order.Id, "Item 2", 50m, 0.3m, 1));   // 50kg
        Assert.Equal(250m, order.TotalWeight);
    }

    [Fact]
    public void TotalWeight_WithTonUnit_ShouldConvertToKg()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "Heavy cargo", 2m, 1m, 1, weightUnit: Ton)); // 2 ton = 2000kg
        Assert.Equal(2000m, order.TotalWeight);
    }

    [Fact]
    public void TotalVolumeCBM_ShouldSumAllItems()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "Item 1", 100m, 0.5m, 2));  // 1.0 CBM
        order.AddItem(OrderItem.Create(order.Id, "Item 2", 50m, 0.3m, 1));   // 0.3 CBM
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

    // ── Amend ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Amend_OnDraftOrder_ShouldUpdatePickup()
    {
        var order = CreateDraftOrder();
        var newPickup = Address.Create("789 ถ.รัชดา", "ห้วยขวาง", "ห้วยขวาง", "กรุงเทพมหานคร", "10310");

        order.Amend(newPickup: newPickup);

        Assert.Equal("789 ถ.รัชดา", order.PickupAddress.Street);
    }

    [Fact]
    public void Amend_OnConfirmedOrder_ShouldSucceed()
    {
        var order = CreateConfirmedOrder();
        var newDropoff = Address.Create("1 ถ.สีลม", "สีลม", "บางรัก", "กรุงเทพมหานคร", "10500");

        order.Amend(newDropoff: newDropoff);

        Assert.Equal("1 ถ.สีลม", order.DropoffAddress.Street);
    }

    [Fact]
    public void Amend_OnPlannedOrder_ShouldThrowDomainException()
    {
        var order = CreateConfirmedOrder();
        order.MarkAsPlanned();
        var newPickup = Address.Create("X", "X", "X", "X", "00000");

        Assert.Throws<DomainException>(() => order.Amend(newPickup: newPickup));
    }

    [Fact]
    public void Amend_ShouldUpdatePriority()
    {
        var order = CreateDraftOrder();
        order.Amend(newPriority: OrderPriority.Express);
        Assert.Equal(OrderPriority.Express, order.Priority);
    }

    [Fact]
    public void Amend_OnConfirmedOrder_ShouldRevertStatusToDraft()
    {
        var order = CreateConfirmedOrder();
        var newDropoff = Address.Create("1 ถ.สีลม", "สีลม", "บางรัก", "กรุงเทพมหานคร", "10500");

        order.Amend(newDropoff: newDropoff);

        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Amend_OnDraftOrder_ShouldKeepStatusDraft()
    {
        var order = CreateDraftOrder();
        var newPickup = Address.Create("789 ถ.รัชดา", "ห้วยขวาง", "ห้วยขวาง", "กรุงเทพมหานคร", "10310");

        order.Amend(newPickup: newPickup);

        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    // ── Dangerous Goods ───────────────────────────────────────────────────────

    [Fact]
    public void OrderItem_DangerousGoods_WithDGClass_ShouldSucceed()
    {
        var order = CreateDraftOrder();
        var item = OrderItem.Create(order.Id, "สารเคมี", 20m, 0.1m, 1,
            isDangerousGoods: true, unNumber: "UN1234", dgClass: "3");

        Assert.True(item.IsDangerousGoods);
        Assert.Equal("3", item.DGClass);
    }

    [Fact]
    public void OrderItem_DangerousGoods_WithoutDGClass_ShouldThrowArgumentException()
    {
        var order = CreateDraftOrder();
        Assert.Throws<ArgumentException>(() =>
            OrderItem.Create(order.Id, "สารเคมี", 20m, 0.1m, 1,
                isDangerousGoods: true, dgClass: null));
    }
}
