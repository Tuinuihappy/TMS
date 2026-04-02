using System.Text.Json;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record RouteStopDto(
    Guid Id, int Sequence, Guid OrderId,
    double Lat, double Lng,
    DateTime? EstimatedArrivalTime, DateTime? EstimatedDepartureTime);

public sealed record RoutePlanDto(
    Guid Id, string PlanNumber, string Status,
    Guid? VehicleTypeId, DateOnly PlannedDate,
    decimal TotalDistanceKm, int EstimatedTotalDurationMin,
    decimal CapacityUtilizationPercent, DateTime CreatedAt,
    List<RouteStopDto> Stops);

public sealed record OptimizationRequestDto(
    Guid Id, string Status, DateTime RequestedAt, DateTime? CompletedAt, string? Error);

public sealed record OrderLocationInput(Guid OrderId, double Lat, double Lng, decimal WeightKg = 0);

// ── Commands ─────────────────────────────────────────────────────────────────

// POST /api/planning/optimize
public sealed record RequestOptimizationCommand(
    List<OrderLocationInput> Orders,
    Guid? VehicleTypeId,
    DateOnly PlannedDate,
    Guid TenantId,
    int MaxStopsPerRoute = 20) : ICommand<Guid>;

public sealed class RequestOptimizationHandler(
    IOptimizationRequestRepository optRepo,
    IRoutePlanRepository planRepo,
    GreedyRouteOptimizer optimizer)
    : ICommandHandler<RequestOptimizationCommand, Guid>
{
    public async Task<Guid> Handle(RequestOptimizationCommand req, CancellationToken ct)
    {
        var paramsJson = JsonSerializer.Serialize(req);
        var optRequest = OptimizationRequest.Create(req.TenantId, paramsJson);
        await optRepo.AddAsync(optRequest, ct);

        // Run greedy optimization synchronously (fast enough for small batches < 100 orders)
        optRequest.MarkProcessing();
        await optRepo.UpdateAsync(optRequest, ct);

        try
        {
            var routes = optimizer.Optimize(req.Orders, req.MaxStopsPerRoute);
            var plans = new List<RoutePlan>();

            for (int i = 0; i < routes.Count; i++)
            {
                var route = routes[i];
                var planNumber = await planRepo.GeneratePlanNumberAsync(ct);
                var plan = RoutePlan.Create(planNumber, req.PlannedDate, req.TenantId, req.VehicleTypeId);

                for (int seq = 0; seq < route.Count; seq++)
                {
                    var o = route[seq];
                    var stop = RouteStop.Create(plan.Id, seq + 1, o.OrderId, o.Lat, o.Lng);
                    plan.AddStop(stop);
                }

                plans.Add(plan);
            }

            await planRepo.AddRangeAsync(plans, ct);
            optRequest.Complete(JsonSerializer.Serialize(new { PlanCount = plans.Count }), plans);
        }
        catch (Exception ex)
        {
            optRequest.Fail(ex.Message);
        }

        await optRepo.UpdateAsync(optRequest, ct);
        return optRequest.Id;
    }
}

// GET /api/planning/optimize/{id}
public sealed record GetOptimizationStatusQuery(Guid RequestId) : IQuery<OptimizationRequestDto?>;

public sealed class GetOptimizationStatusHandler(IOptimizationRequestRepository repo)
    : IQueryHandler<GetOptimizationStatusQuery, OptimizationRequestDto?>
{
    public async Task<OptimizationRequestDto?> Handle(GetOptimizationStatusQuery req, CancellationToken ct)
    {
        var r = await repo.GetByIdAsync(req.RequestId, ct);
        if (r is null) return null;
        return new OptimizationRequestDto(r.Id, r.Status.ToString(), r.RequestedAt, r.CompletedAt, r.ErrorMessage);
    }
}

// GET /api/planning/plans
public sealed record GetRoutePlansQuery(DateOnly? Date, Guid? TenantId) : IQuery<List<RoutePlanDto>>;

public sealed class GetRoutePlansHandler(IRoutePlanRepository repo)
    : IQueryHandler<GetRoutePlansQuery, List<RoutePlanDto>>
{
    public async Task<List<RoutePlanDto>> Handle(GetRoutePlansQuery req, CancellationToken ct)
    {
        var date = req.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var plans = await repo.GetByDateAsync(date, req.TenantId, ct);
        return plans.Select(MapDto).ToList();
    }

    internal static RoutePlanDto MapDto(RoutePlan p) => new(
        p.Id, p.PlanNumber, p.Status.ToString(), p.VehicleTypeId, p.PlannedDate,
        p.TotalDistanceKm, p.EstimatedTotalDurationMin, p.CapacityUtilizationPercent, p.CreatedAt,
        p.Stops.OrderBy(s => s.Sequence)
            .Select(s => new RouteStopDto(s.Id, s.Sequence, s.OrderId, s.Latitude, s.Longitude,
                s.EstimatedArrivalTime, s.EstimatedDepartureTime)).ToList());
}

// GET /api/planning/plans/{id}
public sealed record GetRoutePlanByIdQuery(Guid PlanId) : IQuery<RoutePlanDto?>;

public sealed class GetRoutePlanByIdHandler(IRoutePlanRepository repo)
    : IQueryHandler<GetRoutePlanByIdQuery, RoutePlanDto?>
{
    public async Task<RoutePlanDto?> Handle(GetRoutePlanByIdQuery req, CancellationToken ct)
    {
        var plan = await repo.GetByIdAsync(req.PlanId, ct);
        return plan is null ? null : GetRoutePlansHandler.MapDto(plan);
    }
}

// PUT /api/planning/plans/{id}/stops  (Manual reorder)
public sealed record ReorderStopsInput(Guid StopId, int NewSequence);

public sealed record ReorderStopsCommand(Guid PlanId, List<ReorderStopsInput> Reorder) : ICommand;

public sealed class ReorderStopsHandler(IRoutePlanRepository repo)
    : ICommandHandler<ReorderStopsCommand>
{
    public async Task Handle(ReorderStopsCommand req, CancellationToken ct)
    {
        var plan = await repo.GetByIdAsync(req.PlanId, ct)
            ?? throw new NotFoundException(nameof(RoutePlan), req.PlanId);

        var reordered = plan.Stops
            .Select(s =>
            {
                var input = req.Reorder.FirstOrDefault(r => r.StopId == s.Id);
                if (input is not null) s.UpdateSequence(input.NewSequence);
                return s;
            })
            .OrderBy(s => s.Sequence)
            .ToList();

        plan.SetStops(reordered);
        await repo.UpdateAsync(plan, ct);
    }
}

// PUT /api/planning/plans/{id}/lock
public sealed record LockRoutePlanCommand(Guid PlanId) : ICommand;

public sealed class LockRoutePlanHandler(
    IRoutePlanRepository repo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<LockRoutePlanCommand>
{
    public async Task Handle(LockRoutePlanCommand req, CancellationToken ct)
    {
        var plan = await repo.GetByIdAsync(req.PlanId, ct)
            ?? throw new NotFoundException(nameof(RoutePlan), req.PlanId);

        plan.Lock();
        await repo.UpdateAsync(plan, ct);

        // Publish → Dispatch module will auto-create Trip + Stops
        var stops = plan.Stops.Select(s => new RoutePlanStopSnapshot(
            s.Sequence, s.OrderId, s.Latitude, s.Longitude, s.EstimatedArrivalTime)).ToList();

        await eventPublisher.PublishAsync(
            new RoutePlanLockedIntegrationEvent(
                plan.Id, plan.VehicleTypeId, plan.PlannedDate, plan.TenantId, stops), ct);
    }
}
