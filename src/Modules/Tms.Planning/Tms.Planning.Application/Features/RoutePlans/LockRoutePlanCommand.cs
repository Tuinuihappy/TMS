using MediatR;

namespace Tms.Planning.Application.Features.RoutePlans;

public sealed record LockRoutePlanCommand(
    Guid RoutePlanId) : IRequest;
