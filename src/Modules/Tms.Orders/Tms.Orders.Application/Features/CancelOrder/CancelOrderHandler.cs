using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Application.Features.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : ICommand;

public sealed class CancelOrderHandler(IOrderRepository orderRepository)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransportOrder), request.OrderId);

        order.Cancel(request.Reason);
        await orderRepository.UpdateAsync(order, cancellationToken);
    }
}
