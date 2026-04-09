using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record RouteStopDto(
    Guid Id, int Sequence, Guid OrderId,
    string StopType,
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

/// <summary>Legacy: delivery-only input (ใช้กับ GreedyRouteOptimizer)</summary>
public sealed record OrderLocationInput(Guid OrderId, double Lat, double Lng, decimal WeightKg = 0);

/// <summary>
/// PDP input — 1 Order มีทั้ง Pickup location + Dropoff location
/// รวม volume constraint + time window
/// </summary>
public sealed record PdpOrderInput(
    Guid OrderId,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng,
    decimal WeightKg = 0,
    decimal VolumeCBM = 0,
    DateTime? PickupWindowFrom = null,
    DateTime? PickupWindowTo = null,
    DateTime? DropoffWindowFrom = null,
    DateTime? DropoffWindowTo = null);

// ── Commands ─────────────────────────────────────────────────────────────────

// POST /api/planning/optimize
/// <summary>
/// สร้าง RoutePlan ด้วย PDP Optimizer — Nearest Neighbor with Precedence Constraints
/// </summary>
public sealed record RequestOptimizationCommand(
    List<PdpOrderInput> Orders,
    Guid? VehicleTypeId,
    DateOnly PlannedDate,
    Guid TenantId,
    int MaxOrdersPerRoute = 10,
    decimal MaxCapacityKg = 10_000m,
    decimal MaxCapacityVolumeCBM = 0m,
    double DepotLat = 0,
    double DepotLng = 0,
    DateTime? DepartureTime = null) : ICommand<Guid>;

public sealed class RequestOptimizationHandler(
    IOptimizationRequestRepository optRepo,
    IRoutePlanRepository planRepo,
    OrToolsVrpSolver vrpSolver,
    ILogger<RequestOptimizationHandler> logger)
    : ICommandHandler<RequestOptimizationCommand, Guid>
{
    public async Task<Guid> Handle(RequestOptimizationCommand req, CancellationToken ct)
    {
        var paramsJson = JsonSerializer.Serialize(req);
        var optRequest = OptimizationRequest.Create(req.TenantId, paramsJson);
        await optRepo.AddAsync(optRequest, ct);

        optRequest.MarkProcessing();
        await optRepo.UpdateAsync(optRequest, ct);

        var orderMap = req.Orders.ToDictionary(o => o.OrderId);

        try
        {
            // Convert PdpOrderInput → VrpOrderInput for OR-Tools solver
            var vrpInputs = req.Orders.Select(o => new VrpOrderInput(
                OrderId: o.OrderId,
                PickupLat: o.PickupLat,
                PickupLng: o.PickupLng,
                DropoffLat: o.DropoffLat,
                DropoffLng: o.DropoffLng,
                WeightKg: o.WeightKg,
                VolumeCbm: o.VolumeCBM,
                PickupWindowFrom: o.PickupWindowFrom,
                PickupWindowTo: o.PickupWindowTo,
                DropoffWindowFrom: o.DropoffWindowFrom,
                DropoffWindowTo: o.DropoffWindowTo
            )).ToList();

            // Determine number of vehicles based on total weight vs capacity
            var totalWeightKg = vrpInputs.Sum(o => o.WeightKg);
            var numVehicles = Math.Max(1, (int)Math.Ceiling(totalWeightKg / req.MaxCapacityKg));
            // Also consider order count constraint
            numVehicles = Math.Max(numVehicles, (int)Math.Ceiling(vrpInputs.Count / (double)req.MaxOrdersPerRoute));

            var depotLat = req.DepotLat == 0 ? 13.7563 : req.DepotLat;  // Default: Bangkok
            var depotLng = req.DepotLng == 0 ? 100.5018 : req.DepotLng;

            logger.LogInformation(
                "Invoking OR-Tools VRP Solver: {OrderCount} orders, {Vehicles} vehicles, capacity={CapKg}kg, depot=({Lat},{Lng})",
                vrpInputs.Count, numVehicles, req.MaxCapacityKg, depotLat, depotLng);

            // Invoke Google OR-Tools VRP Solver
            var vrpRoutes = vrpSolver.Solve(
                orders: vrpInputs,
                numVehicles: numVehicles,
                vehicleCapacityKg: req.MaxCapacityKg,
                maxDistancePerVehicleKm: 500m,
                timeLimitSeconds: 15,
                depotLat: depotLat,
                depotLng: depotLng,
                departureTime: req.DepartureTime,
                logger: logger);

            logger.LogInformation("OR-Tools returned {RouteCount} route(s) for {OrderCount} orders.",
                vrpRoutes.Count, vrpInputs.Count);

            // Map VRP results → RoutePlans
            var plans = new List<RoutePlan>();

            foreach (var vrpRoute in vrpRoutes)
            {
                var planNumber = await planRepo.GeneratePlanNumberAsync(ct);
                var plan = RoutePlan.Create(planNumber, req.PlannedDate, req.TenantId, req.VehicleTypeId);

                // Build stops from VRP result (already optimally ordered)
                int seq = 1;
                foreach (var vrpStop in vrpRoute.Stops)
                {
                    var stop = RouteStop.Create(
                        plan.Id, seq++, vrpStop.OrderId,
                        vrpStop.StopType,
                        vrpStop.Latitude, vrpStop.Longitude,
                        vrpStop.EstimatedArrival);
                    plan.AddStop(stop);
                }

                // Use metrics computed by VRP solver
                var pickupWeight = vrpRoute.Stops
                    .Where(s => s.StopType == "Pickup")
                    .Sum(s => orderMap.TryGetValue(s.OrderId, out var o) ? o.WeightKg : 0);
                var capUtil = req.MaxCapacityKg > 0
                    ? Math.Round(pickupWeight / req.MaxCapacityKg * 100, 1)
                    : 0m;

                plan.UpdateMetrics(vrpRoute.TotalDistanceKm, vrpRoute.EstimatedDurationMin, capUtil);
                plans.Add(plan);
            }

            await planRepo.AddRangeAsync(plans, ct);
            optRequest.Complete(
                JsonSerializer.Serialize(new
                {
                    PlanCount = plans.Count,
                    Solver = "Google OR-Tools",
                    TotalOrders = vrpInputs.Count,
                    Vehicles = numVehicles
                }), plans);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OR-Tools optimization failed for request {Id}.", optRequest.Id);
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
            .Select(s => new RouteStopDto(
                s.Id, s.Sequence, s.OrderId, s.StopType,
                s.Latitude, s.Longitude,
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
    IOutboxWriter outbox)
    : ICommandHandler<LockRoutePlanCommand>
{
    public async Task Handle(LockRoutePlanCommand req, CancellationToken ct)
    {
        var plan = await repo.GetByIdAsync(req.PlanId, ct)
            ?? throw new NotFoundException(nameof(RoutePlan), req.PlanId);

        plan.Lock();

        var stops = plan.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new RoutePlanStopSnapshot(
                s.Sequence, s.OrderId,
                s.StopType,
                s.Latitude, s.Longitude,
                s.EstimatedArrivalTime))
            .ToList();

        // Stage event BEFORE SaveChanges — written atomically with the Plan update
        outbox.Stage(new RoutePlanLockedIntegrationEvent(
            plan.Id, plan.VehicleTypeId, plan.PlannedDate, plan.TenantId, stops));

        await repo.UpdateAsync(plan, ct);
    }
}
