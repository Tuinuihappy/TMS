using Tms.Orders.Application;
using Tms.Orders.Infrastructure.Persistence;
using Tms.SharedKernel.Application;


namespace Tms.Orders.Infrastructure;

public sealed class OrdersOutboxWriter(
    OrdersDbContext ctx) : IOrdersOutboxWriter
{
    private readonly OutboxWriter<OrdersDbContext> _inner = new(ctx);
    public void Stage(IIntegrationEvent @event) => _inner.Stage(@event);
    public void StageRange(IEnumerable<IIntegrationEvent> events) => _inner.StageRange(events);
}
