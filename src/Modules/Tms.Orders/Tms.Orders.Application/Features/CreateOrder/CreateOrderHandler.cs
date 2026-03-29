using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed class CreateOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var orderNumber = request.OrderNumber
            ?? await orderRepository.GenerateOrderNumberAsync(cancellationToken);

        var pickup = Address.Create(
            request.PickupAddress.Street,
            request.PickupAddress.SubDistrict,
            request.PickupAddress.District,
            request.PickupAddress.Province,
            request.PickupAddress.PostalCode,
            request.PickupAddress.Latitude,
            request.PickupAddress.Longitude);

        var dropoff = Address.Create(
            request.DropoffAddress.Street,
            request.DropoffAddress.SubDistrict,
            request.DropoffAddress.District,
            request.DropoffAddress.Province,
            request.DropoffAddress.PostalCode,
            request.DropoffAddress.Latitude,
            request.DropoffAddress.Longitude);

        var priority = Enum.TryParse<OrderPriority>(request.Priority, true, out var p)
            ? p : OrderPriority.Normal;

        var pickupWindow = request.PickupWindow is not null
            ? TimeWindow.Create(request.PickupWindow.From, request.PickupWindow.To)
            : null;

        var dropoffWindow = request.DropoffWindow is not null
            ? TimeWindow.Create(request.DropoffWindow.From, request.DropoffWindow.To)
            : null;

        var order = TransportOrder.Create(
            orderNumber, request.CustomerId,
            pickup, dropoff,
            priority, pickupWindow, dropoffWindow,
            request.Notes);

        foreach (var itemDto in request.Items)
        {
            var item = OrderItem.Create(
                order.Id,
                itemDto.Description,
                itemDto.Weight,
                itemDto.Volume,
                itemDto.Quantity);
            order.AddItem(item);
        }

        await orderRepository.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
