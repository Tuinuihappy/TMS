using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Application.Features.ConfirmOrder;

public sealed record ConfirmOrderCommand(Guid OrderId) : ICommand, IAuditableCommand
{
    public string ResourceName => "TransportOrder";
    public string? ResourceId => OrderId.ToString();
}

public sealed class ConfirmOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<ConfirmOrderCommand>
{
    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        order.Confirm();
        await orderRepository.UpdateAsync(order, cancellationToken);
    }
}
