using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed class CreateOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<CreateOrderCommand, CreateOrderResponse>
{
    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
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
            request.PickupAddress.Longitude,
            request.PickupAddress.Name);

        var dropoff = Address.Create(
            request.DropoffAddress.Street,
            request.DropoffAddress.SubDistrict,
            request.DropoffAddress.District,
            request.DropoffAddress.Province,
            request.DropoffAddress.PostalCode,
            request.DropoffAddress.Latitude,
            request.DropoffAddress.Longitude,
            request.DropoffAddress.Name);

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
            request.Notes, request.TenantId);

        foreach (var itemDto in request.Items)
        {
            var item = OrderItem.Create(
                order.Id,
                itemDto.Description,
                itemDto.WeightKg,
                itemDto.VolumeCBM,
                itemDto.Quantity,
                sku: itemDto.Sku,
                isDangerousGoods: itemDto.IsDangerousGoods,
                unNumber: itemDto.UNNumber,
                dgClass: itemDto.DGClass);
            order.AddItem(item);
        }

        await orderRepository.AddAsync(order, cancellationToken);

        return new CreateOrderResponse(
            order.Id,
            order.OrderNumber,
            order.Status.ToString(),
            order.TotalWeight,
            order.TotalVolumeCBM,
            order.CreatedAt);
    }
}
