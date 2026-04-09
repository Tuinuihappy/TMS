using MediatR;
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
/// 3. ส่ง child orders เข้า PDP optimizer
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
    PdpRouteOptimizer optimizer)
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

        // 4. Build PdpOrderInput list
        var pdpInputs = effectiveSnapshots
            .Where(s => s.PickupLat.HasValue && s.DropoffLat.HasValue)
            .Select(s => new PdpOrderInput(
                s.Id,
                s.PickupLat!.Value, s.PickupLng!.Value,
                s.DropoffLat!.Value, s.DropoffLng!.Value,
                s.TotalWeightKg, s.TotalVolumeCBM,
                s.PickupWindowFrom, s.PickupWindowTo,
                s.DropoffWindowFrom, s.DropoffWindowTo))
            .ToList();

        if (pdpInputs.Count == 0)
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
            var orderMap = pdpInputs.ToDictionary(o => o.OrderId);

            var routes = optimizer.Optimize(
                pdpInputs,
                request.MaxOrdersPerRoute,
                request.MaxVehicleWeightKg,
                request.MaxVehicleVolumeCBM,
                request.DepotLat,
                request.DepotLng,
                request.DepartureTime);

            var plans = new List<RoutePlan>();

            for (int i = 0; i < routes.Count; i++)
            {
                var route = routes[i];
                var planNumber = await planRepo.GeneratePlanNumberAsync(ct);
                var plan = RoutePlan.Create(
                    planNumber, request.PlannedDate, request.TenantId, request.VehicleTypeId);

                for (int seq = 0; seq < route.Count; seq++)
                {
                    var pdpStop = route[seq];
                    var stop = RouteStop.Create(
                        plan.Id, seq + 1, pdpStop.OrderId,
                        pdpStop.StopType,
                        pdpStop.Lat, pdpStop.Lng);
                    plan.AddStop(stop);
                }

                var distKm = PdpRouteOptimizer.ComputeTotalDistanceKm(
                    route, request.DepotLat, request.DepotLng);
                var durationMin = PdpRouteOptimizer.EstimateDurationMin(distKm);
                var capUtil = PdpRouteOptimizer.ComputeCapacityUtil(
                    route, orderMap, request.MaxVehicleWeightKg);

                plan.UpdateMetrics(distKm, durationMin, capUtil);
                plans.Add(plan);
            }

            await planRepo.AddRangeAsync(plans, ct);
            optRequest.Complete(
                System.Text.Json.JsonSerializer.Serialize(new { PlanCount = plans.Count }),
                plans);

            await optRepo.UpdateAsync(optRequest, ct);

            return new PlanWithSplitResult(optRequest.Id, plans.Count, splitsPerformed);
        }
        catch (Exception ex)
        {
            optRequest.Fail(ex.Message);
            await optRepo.UpdateAsync(optRequest, ct);
            throw;
        }
    }
}
