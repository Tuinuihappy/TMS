using MediatR;

namespace Tms.Planning.Application.Features.AutoPlanning;

public sealed record StartAutoOptimizationCommand(
    Guid SessionId,
    List<Guid> PlanningOrderIds,
    Guid TenantId) : IRequest;
