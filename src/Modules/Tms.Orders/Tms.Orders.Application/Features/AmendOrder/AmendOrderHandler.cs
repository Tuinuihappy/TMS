using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Application.Features.AmendOrder;

public sealed record AmendOrderRequest(
    AddressDto? PickupAddress = null,
    AddressDto? DropoffAddress = null,
    TimeWindowDto? PickupWindow = null,
    TimeWindowDto? DropoffWindow = null,
    string? Priority = null,
    string? Notes = null);

// Reuse DTOs from CreateOrder
public sealed record AddressDto(
    string Street, string SubDistrict, string District,
    string Province, string PostalCode,
    double? Latitude = null, double? Longitude = null);

public sealed record TimeWindowDto(DateTime From, DateTime To);

public sealed record AmendOrderCommand(
    Guid OrderId,
    AmendOrderRequest Request) : ICommand;

public sealed class AmendOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<AmendOrderCommand>
{
    public async Task Handle(AmendOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        var req = request.Request;

        var newPickup = req.PickupAddress is not null
            ? Address.Create(req.PickupAddress.Street, req.PickupAddress.SubDistrict,
                req.PickupAddress.District, req.PickupAddress.Province,
                req.PickupAddress.PostalCode, req.PickupAddress.Latitude, req.PickupAddress.Longitude)
            : null;

        var newDropoff = req.DropoffAddress is not null
            ? Address.Create(req.DropoffAddress.Street, req.DropoffAddress.SubDistrict,
                req.DropoffAddress.District, req.DropoffAddress.Province,
                req.DropoffAddress.PostalCode, req.DropoffAddress.Latitude, req.DropoffAddress.Longitude)
            : null;

        var newPickupWindow = req.PickupWindow is not null
            ? TimeWindow.Create(req.PickupWindow.From, req.PickupWindow.To)
            : null;

        var newDropoffWindow = req.DropoffWindow is not null
            ? TimeWindow.Create(req.DropoffWindow.From, req.DropoffWindow.To)
            : null;

        var newPriority = req.Priority is not null
            ? Enum.Parse<OrderPriority>(req.Priority, ignoreCase: true)
            : (OrderPriority?)null;

        order.Amend(newPickup, newDropoff, newPickupWindow, newDropoffWindow,
            req.Notes, newPriority);

        await orderRepository.UpdateAsync(order, cancellationToken);
    }
}
