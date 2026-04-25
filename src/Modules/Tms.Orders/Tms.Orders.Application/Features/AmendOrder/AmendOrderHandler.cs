using Tms.Orders.Application.Abstractions;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Orders.Application.Features.AmendOrder;


public sealed record AmendOrderRequest(
    AddressDto? PickupAddress = null,
    AddressDto? DropoffAddress = null,
    TimeWindowDto? PickupWindow = null,
    TimeWindowDto? DropoffWindow = null,
    string? Priority = null,
    string? Notes = null);

public sealed record AmendOrderCommand(
    Guid OrderId,
    AmendOrderRequest Request) : ICommand;

public sealed class AmendOrderHandler(
    IOrderRepository orderRepository,
    IOutboxWriter outbox,
    IOrderCacheInvalidator cacheInvalidator)
    : ICommandHandler<AmendOrderCommand>
{
    public async Task Handle(AmendOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        var req = request.Request;

        var changes = new List<string>();
        if (req.PickupAddress is not null || req.DropoffAddress is not null) changes.Add("address");
        if (req.PickupWindow is not null || req.DropoffWindow is not null) changes.Add("timeWindow");
        if (req.Priority is not null || req.Notes is not null) changes.Add("details");

        var newPickup = req.PickupAddress is not null
            ? Address.Create(req.PickupAddress.Street, req.PickupAddress.SubDistrict,
                req.PickupAddress.District, req.PickupAddress.Province,
                req.PickupAddress.PostalCode, req.PickupAddress.Latitude, req.PickupAddress.Longitude,
                req.PickupAddress.Name)
            : null;

        var newDropoff = req.DropoffAddress is not null
            ? Address.Create(req.DropoffAddress.Street, req.DropoffAddress.SubDistrict,
                req.DropoffAddress.District, req.DropoffAddress.Province,
                req.DropoffAddress.PostalCode, req.DropoffAddress.Latitude, req.DropoffAddress.Longitude,
                req.DropoffAddress.Name)
            : null;

        var newPickupWindow = req.PickupWindow is not null
            ? TimeWindow.Create(req.PickupWindow.From, req.PickupWindow.To)
            : null;

        var newDropoffWindow = req.DropoffWindow is not null
            ? TimeWindow.Create(req.DropoffWindow.From, req.DropoffWindow.To)
            : null;

        var newPriority = req.Priority is not null && Enum.TryParse<OrderPriority>(req.Priority, ignoreCase: true, out var p)
            ? p
            : (OrderPriority?)null;

        order.Amend(newPickup, newDropoff, newPickupWindow, newDropoffWindow,
            req.Notes, newPriority);

        outbox.Stage(new OrderAmendedIntegrationEvent(order.Id, order.OrderNumber, changes));

        await orderRepository.UpdateAsync(order, cancellationToken);

        // Invalidate cached OrderSnapshot so Planning module gets fresh data
        // on the next OrderAmendedIntegrationEvent handling
        await cacheInvalidator.InvalidateAsync(order.Id, cancellationToken);
    }
}
