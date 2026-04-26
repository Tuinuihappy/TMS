using Tms.Planning.Application;
using Tms.Planning.Infrastructure.Persistence;
using Tms.SharedKernel.Application;


namespace Tms.Planning.Infrastructure;

public sealed class PlanningOutboxWriter(
    PlanningDbContext ctx) : IPlanningOutboxWriter
{
    private readonly OutboxWriter<PlanningDbContext> _inner = new(ctx);
    public void Stage(IIntegrationEvent @event) => _inner.Stage(@event);
    public void StageRange(IEnumerable<IIntegrationEvent> events) => _inner.StageRange(events);
}
