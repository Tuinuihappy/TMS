using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.ValueObjects;
using Xunit;

namespace Tms.Orders.UnitTests.Domain;

public sealed class TransportOrderTests
{
    private static TransportOrder CreateDraftOrder()
    {
        var pickup = Address.Create("123 ถ.สุขุมวิท", "คลองเตย", "คลองเตย", "กรุงเทพมหานคร", "10110");
        var dropoff = Address.Create("456 ถ.พหลโยธิน", "จตุจักร", "จตุจักร", "กรุงเทพมหานคร", "10900");
        return TransportOrder.Create("ORD-TEST-001", Guid.NewGuid(), pickup, dropoff);
    }

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        var order = CreateDraftOrder();
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Confirm_WithItems_ShouldChangeStatusToConfirmed()
    {
        var order = CreateDraftOrder();
        var item = OrderItem.Create(order.Id, "สินค้าทดสอบ", 100m, 0.5m, 2);
        order.AddItem(item);

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WithoutItems_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
        Assert.Throws<Tms.SharedKernel.Exceptions.DomainException>(() => order.Confirm());
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        var order = CreateDraftOrder();
        order.Cancel("ยกเลิกโดยลูกค้า");
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void AddItem_ToConfirmedOrder_ShouldThrowDomainException()
    {
        var order = CreateDraftOrder();
        var item = OrderItem.Create(order.Id, "First item", 50m, 0.2m, 1);
        order.AddItem(item);
        order.Confirm();

        var newItem = OrderItem.Create(order.Id, "Second item", 30m, 0.1m, 1);
        Assert.Throws<Tms.SharedKernel.Exceptions.DomainException>(() => order.AddItem(newItem));
    }

    [Fact]
    public void TotalWeight_ShouldSumAllItems()
    {
        var order = CreateDraftOrder();
        order.AddItem(OrderItem.Create(order.Id, "Item 1", 100m, 0.5m, 2));  // 200kg
        order.AddItem(OrderItem.Create(order.Id, "Item 2", 50m, 0.3m, 1));   // 50kg
        Assert.Equal(250m, order.TotalWeight);
    }
}
