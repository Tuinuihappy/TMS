    using MediatR;

namespace Tms.Planning.Application.Features.AutoPlanning;

public sealed record ProcessOptimizationRequestCommand(
    Guid OptimizationRequestId) : IRequest;
