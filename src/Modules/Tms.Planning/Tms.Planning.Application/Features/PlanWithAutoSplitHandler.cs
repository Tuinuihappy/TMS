using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Planning.Application.Features;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// POST /api/planning/plan-with-split
/// Auto Planning + Auto Split รวมกัน:
/// 1. โหลด orders ตาม OrderIds
/// 2. ตรวจ orders ไหนเกิน capacity → auto split ก่อน (ผ่าน MediatR cross-module)
/// 3. ส่ง child orders เข้า OR-Tools VRP Solver
/// 4. สร้าง RoutePlans
/// </summary>
public sealed record PlanWithAutoSplitCommand(
    List<Guid> OrderIds,
    DateOnly PlannedDate,
    Guid TenantId,
    decimal MaxVehicleWeightKg = 10_000m,
    decimal MaxVehicleVolumeCBM = 0m,
    int MaxOrdersPerRoute = 10,
    Guid? VehicleTypeId = null,
    double DepotLat = 0,
    double DepotLng = 0,
    DateTime? DepartureTime = null) : ICommand<PlanWithSplitResult>;

// ── Result ───────────────────────────────────────────────────────────────────

public sealed record PlanWithSplitResult(
    Guid OptimizationRequestId,
    int RoutePlanCount,
    List<SplitPerformedSummary> SplitsPerformed);

public sealed record SplitPerformedSummary(
    Guid OriginalOrderId,
    string OriginalOrderNumber,
    List<Guid> SplitChildOrderIds);

/// <summary>
/// Cross-module split request bridged via MediatR — actual type lives in SharedKernel.
/// Handler (CrossModuleAutoSplitHandler) lives in Orders.Application.
/// </summary>

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class PlanWithAutoSplitHandler(
    IOrderQueryService orderQuery,
    ISender mediator,
    IOptimizationRequestRepository optRepo,
    IRoutePlanRepository planRepo,
    OrToolsVrpSolver vrpSolver,
    ILogger<PlanWithAutoSplitHandler> logger)
    : ICommandHandler<PlanWithAutoSplitCommand, PlanWithSplitResult>
{
    public async Task<PlanWithSplitResult> Handle(
        PlanWithAutoSplitCommand request, CancellationToken ct)
    {
        // 1. Load orders
        var snapshots = await orderQuery.GetOrdersByIdsAsync(request.OrderIds, ct);
        if (snapshots.Count == 0)
            throw new NotFoundException("Orders", "none found");

        // Validate status
        var invalid = snapshots
            .Where(o => o.Status is not ("Confirmed" or "Draft" or "PartialSplit"))
            .ToList();
        if (invalid.Count > 0)
            throw new DomainException(
                $"Orders [{string.Join(", ", invalid.Select(o => o.OrderNumber))}] cannot be planned.",
                "INVALID_ORDER_STATUS_FOR_PLAN");

        // 2. Auto-split oversized orders (via MediatR cross-module)
        var splitsPerformed = new List<SplitPerformedSummary>();
        var effectiveOrderIds = new List<Guid>();

        foreach (var snap in snapshots)
        {
            bool exceedsWeight = request.MaxVehicleWeightKg > 0
                                 && snap.TotalWeightKg > request.MaxVehicleWeightKg;
            bool exceedsVolume = request.MaxVehicleVolumeCBM > 0
                                 && snap.TotalVolumeCBM > request.MaxVehicleVolumeCBM;

            if (exceedsWeight || exceedsVolume)
            {
                // Send cross-module split request via MediatR
                var splitResult = await mediator.Send(
                    new CrossModuleAutoSplitRequest(
                        snap.Id,
                        request.MaxVehicleWeightKg,
                        request.MaxVehicleVolumeCBM), ct);

                splitsPerformed.Add(new SplitPerformedSummary(
                    snap.Id, snap.OrderNumber, splitResult.ChildOrderIds));
                effectiveOrderIds.AddRange(splitResult.ChildOrderIds);
            }
            else
            {
                effectiveOrderIds.Add(snap.Id);
            }
        }

        // 3. Re-load effective orders (includes newly split children)
        var effectiveSnapshots = await orderQuery.GetOrdersByIdsAsync(effectiveOrderIds, ct);

        // 4. Build VrpOrderInput list for OR-Tools
        var vrpInputs = effectiveSnapshots
            .Where(s => s.PickupLat.HasValue && s.DropoffLat.HasValue)
            .Select(s => new VrpOrderInput(
                OrderId: s.Id,
                PickupLat: s.PickupLat!.Value,
                PickupLng: s.PickupLng!.Value,
                DropoffLat: s.DropoffLat!.Value,
                DropoffLng: s.DropoffLng!.Value,
                WeightKg: s.TotalWeightKg,
                VolumeCbm: s.TotalVolumeCBM,
                PickupWindowFrom: s.PickupWindowFrom,
                PickupWindowTo: s.PickupWindowTo,
                DropoffWindowFrom: s.DropoffWindowFrom,
                DropoffWindowTo: s.DropoffWindowTo))
            .ToList();

        if (vrpInputs.Count == 0)
            throw new DomainException(
                "No orders with valid GPS coordinates to optimize.", "NO_GEOCODED_ORDERS");

        // 5. Record optimization request
        var paramsJson = System.Text.Json.JsonSerializer.Serialize(request);
        var optRequest = OptimizationRequest.Create(request.TenantId, paramsJson);
        await optRepo.AddAsync(optRequest, ct);
        optRequest.MarkProcessing();
        await optRepo.UpdateAsync(optRequest, ct);

        try
        {
            var orderMap = vrpInputs.ToDictionary(o => o.OrderId);

            // Determine number of vehicles
            var totalWeightKg = vrpInputs.Sum(o => o.WeightKg);
            var numVehicles = Math.Max(1, (int)Math.Ceiling(totalWeightKg / request.MaxVehicleWeightKg));
            numVehicles = Math.Max(numVehicles, (int)Math.Ceiling(vrpInputs.Count / (double)request.MaxOrdersPerRoute));

            var depotLat = request.DepotLat == 0 ? 13.7563 : request.DepotLat;
            var depotLng = request.DepotLng == 0 ? 100.5018 : request.DepotLng;

            logger.LogInformation(
                "PlanWithSplit: Invoking OR-Tools VRP — {Orders} orders, {Vehicles} vehicles, capacity={Cap}kg",
                vrpInputs.Count, numVehicles, request.MaxVehicleWeightKg);

            // Invoke Google OR-Tools VRP Solver
            var vrpRoutes = vrpSolver.Solve(
                orders: vrpInputs,
                numVehicles: numVehicles,
                vehicleCapacityKg: request.MaxVehicleWeightKg,
                maxDistancePerVehicleKm: 500m,
                timeLimitSeconds: 15,
                depotLat: depotLat,
                depotLng: depotLng,
                departureTime: request.DepartureTime,
                logger: logger);

            // Map VRP results → RoutePlans
            var plans = new List<RoutePlan>();

            foreach (var vrpRoute in vrpRoutes)
            {
                var planNumber = await planRepo.GeneratePlanNumberAsync(ct);
                var plan = RoutePlan.Create(
                    planNumber, request.PlannedDate, request.TenantId, request.VehicleTypeId);

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

                var pickupWeight = vrpRoute.Stops
                    .Where(s => s.StopType == "Pickup")
                    .Sum(s => orderMap.TryGetValue(s.OrderId, out var o) ? o.WeightKg : 0);
                var capUtil = request.MaxVehicleWeightKg > 0
                    ? Math.Round(pickupWeight / request.MaxVehicleWeightKg * 100, 1)
                    : 0m;

                plan.UpdateMetrics(vrpRoute.TotalDistanceKm, vrpRoute.EstimatedDurationMin, capUtil);
                plans.Add(plan);
            }

            await planRepo.AddRangeAsync(plans, ct);
            optRequest.Complete(
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    PlanCount = plans.Count,
                    Solver = "Google OR-Tools",
                    SplitsPerformed = splitsPerformed.Count
                }),
                plans);

            await optRepo.UpdateAsync(optRequest, ct);

            return new PlanWithSplitResult(optRequest.Id, plans.Count, splitsPerformed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PlanWithSplit OR-Tools optimization failed.");
            optRequest.Fail(ex.Message);
            await optRepo.UpdateAsync(optRequest, ct);
            throw;
        }
    }
}
