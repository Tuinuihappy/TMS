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

        var priority = Enum.TryParse<OrderPriority>(request.Priority, true, out var p)
            ? p : OrderPriority.Normal;

        var order = TransportOrder.Create(
            orderNumber, request.CustomerId, priority, request.Notes, request.TenantId);

        int sequence = 1;
        foreach (var stopDto in request.Stops)
        {
            var seq = stopDto.Sequence > 0 ? stopDto.Sequence : sequence++;

            var pickup = Address.Create(
                stopDto.PickupAddress.Street, stopDto.PickupAddress.SubDistrict,
                stopDto.PickupAddress.District, stopDto.PickupAddress.Province,
                stopDto.PickupAddress.PostalCode,
                stopDto.PickupAddress.Latitude, stopDto.PickupAddress.Longitude,
                stopDto.PickupAddress.Name);

            var dropoff = Address.Create(
                stopDto.DropoffAddress.Street, stopDto.DropoffAddress.SubDistrict,
                stopDto.DropoffAddress.District, stopDto.DropoffAddress.Province,
                stopDto.DropoffAddress.PostalCode,
                stopDto.DropoffAddress.Latitude, stopDto.DropoffAddress.Longitude,
                stopDto.DropoffAddress.Name);

            var pickupWindow = stopDto.PickupWindow is not null
                ? TimeWindow.Create(stopDto.PickupWindow.From, stopDto.PickupWindow.To)
                : null;

            var dropoffWindow = stopDto.DropoffWindow is not null
                ? TimeWindow.Create(stopDto.DropoffWindow.From, stopDto.DropoffWindow.To)
                : null;

            var stop = OrderStop.Create(order.Id, seq, pickup, dropoff, pickupWindow, dropoffWindow);

            foreach (var itemDto in stopDto.Items)
            {
                var item = OrderItem.Create(
                    stop.Id,
                    itemDto.Description,
                    itemDto.WeightKg,
                    itemDto.VolumeCBM,
                    itemDto.Quantity,
                    sku: itemDto.Sku,
                    isDangerousGoods: itemDto.IsDangerousGoods,
                    unNumber: itemDto.UNNumber,
                    dgClass: itemDto.DGClass);
                stop.AddItem(item);
            }

            order.AddStop(stop);
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
