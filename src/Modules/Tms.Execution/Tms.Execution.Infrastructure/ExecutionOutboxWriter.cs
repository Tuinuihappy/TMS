using Tms.Execution.Application;
using Tms.Execution.Infrastructure.Persistence;
using Tms.SharedKernel.Application;


namespace Tms.Execution.Infrastructure;

public sealed class ExecutionOutboxWriter(
    ExecutionDbContext ctx) : IExecutionOutboxWriter
{
    private readonly OutboxWriter<ExecutionDbContext> _inner = new(ctx);
    public void Stage(IIntegrationEvent @event) => _inner.Stage(@event);
    public void StageRange(IEnumerable<IIntegrationEvent> events) => _inner.StageRange(events);
}
