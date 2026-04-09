using MediatR;
using Tms.Planning.Application.Features;
using Tms.SharedKernel.Application;

namespace Tms.WebApi.Endpoints;

public static class RoutePlanEndpoints
{
    public static IEndpointRouteBuilder MapRoutePlanEndpoints(this IEndpointRouteBuilder app)
    {
        var optimize = app.MapGroup("/api/planning/optimize").WithTags("RoutePlanning");
        var plans = app.MapGroup("/api/planning/plans").WithTags("RoutePlanning");

        // POST /api/planning/optimize
        optimize.MapPost("/", async (
            PdpOptimizeRequest req, ISender sender, CancellationToken ct) =>
        {
            var orders = req.Orders.Select(o =>
                new PdpOrderInput(
                    o.OrderId,
                    o.PickupLat, o.PickupLng,
                    o.DropoffLat, o.DropoffLng,
                    o.WeightKg,
                    o.VolumeCBM,
                    o.PickupWindowFrom, o.PickupWindowTo,
                    o.DropoffWindowFrom, o.DropoffWindowTo)).ToList();

            var requestId = await sender.Send(new RequestOptimizationCommand(
                orders,
                req.VehicleTypeId,
                req.PlannedDate,
                req.TenantId,
                req.MaxOrdersPerRoute,
                req.MaxCapacityKg,
                req.MaxCapacityVolumeCBM,
                req.DepotLat,
                req.DepotLng,
                req.DepartureTime), ct);

            return Results.Accepted($"/api/planning/optimize/{requestId}",
                new { OptimizationRequestId = requestId, Status = "Pending" });
        })
        .WithName("RequestRouteOptimization")
        .WithSummary("ส่งคำขอ PDP Route Optimization (Pickup + Dropoff)");

        // GET /api/planning/optimize/{id}
        optimize.MapGet("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOptimizationStatusQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetOptimizationStatus")
        .WithSummary("ดูสถานะ Optimization Request");

        // GET /api/planning/plans
        plans.MapGet("/", async (
            ISender sender,
            DateOnly? date, Guid? tenantId,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRoutePlansQuery(date, tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetRoutePlans")
        .WithSummary("รายการ Route Plans");

        // GET /api/planning/plans/{id}
        plans.MapGet("/{id:guid}", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRoutePlanByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetRoutePlanById")
        .WithSummary("รายละเอียด Route Plan + Stops");

        // PUT /api/planning/plans/{id}/stops  — Reorder stops
        plans.MapPut("/{id:guid}/stops", async (
            Guid id, ReorderStopsRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReorderStopsCommand(
                id,
                req.Reorder.Select(r => new ReorderStopsInput(r.StopId, r.NewSequence)).ToList()), ct);
            return Results.NoContent();
        })
        .WithName("ReorderRoutePlanStops")
        .WithSummary("สลับลำดับ Stop (Manual Adjustment)");

        // PUT /api/planning/plans/{id}/lock  — Lock & auto-create Trip
        plans.MapPut("/{id:guid}/lock", async (
            Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new LockRoutePlanCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("LockRoutePlan")
        .WithSummary("ยืนยัน Plan → สร้าง Trip อัตโนมัติ");

        // POST /api/planning/trips/{id}/reoptimize — Mid-day re-optimization
        var trips = app.MapGroup("/api/planning/trips").WithTags("RoutePlanning");
        trips.MapPost("/{id:guid}/reoptimize", async (
            Guid id, ReOptimizeTripRequest? req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ReOptimizeTripCommand(
                id,
                req?.DepotLat ?? 0,
                req?.DepotLng ?? 0,
                req?.MaxCapacityKg ?? 10_000m,
                req?.MaxCapacityVolumeCBM ?? 0m,
                req?.DepartureTime), ct);
            return Results.NoContent();
        })
        .WithName("ReOptimizeTrip")
        .WithSummary("Re-optimize remaining stops mid-trip execution");

        // POST /api/planning/plan-with-split
        // Auto Planning + Auto Split — ตรวจหาก order เกิน capacity แล้ว split อัตโนมัติก่อน optimize
        var planGroup = app.MapGroup("/api/planning").WithTags("RoutePlanning");
        planGroup.MapPost("/plan-with-split", async (
            PlanWithSplitRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PlanWithAutoSplitCommand(
                req.OrderIds,
                req.PlannedDate,
                req.TenantId,
                req.MaxVehicleWeightKg,
                req.MaxVehicleVolumeCBM,
                req.MaxOrdersPerRoute,
                req.VehicleTypeId,
                req.DepotLat,
                req.DepotLng,
                req.DepartureTime), ct);
            return Results.Ok(result);
        })
        .WithName("PlanWithAutoSplit")
        .WithSummary("Auto Planning + Auto Split — สร้าง Route Plans พร้อม Split Orders ที่เกิน Capacity");

        return app;
    }
}

/// <summary>PDP Order input — ต่อ Order มีทั้ง Pickup และ Dropoff location + volume + time windows</summary>
public sealed record PdpOrderRequest(
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

/// <summary>Request body สำหรับ POST /api/planning/optimize</summary>
public sealed record PdpOptimizeRequest(
    List<PdpOrderRequest> Orders,
    Guid TenantId,
    DateOnly PlannedDate,
    Guid? VehicleTypeId = null,
    int MaxOrdersPerRoute = 10,
    decimal MaxCapacityKg = 10_000m,
    decimal MaxCapacityVolumeCBM = 0m,
    double DepotLat = 0,
    double DepotLng = 0,
    DateTime? DepartureTime = null);

public sealed record ReOrderStopRequest(Guid StopId, int NewSequence);
public sealed record ReorderStopsRequest(List<ReOrderStopRequest> Reorder);
public sealed record ReOptimizeTripRequest(
    double DepotLat = 0,
    double DepotLng = 0,
    decimal MaxCapacityKg = 10_000m,
    decimal MaxCapacityVolumeCBM = 0m,
    DateTime? DepartureTime = null);

/// <summary>Request body for POST /api/planning/plan-with-split</summary>
public sealed record PlanWithSplitRequest(
    List<Guid> OrderIds,
    DateOnly PlannedDate,
    Guid TenantId,
    decimal MaxVehicleWeightKg = 10_000m,
    decimal MaxVehicleVolumeCBM = 0m,
    int MaxOrdersPerRoute = 10,
    Guid? VehicleTypeId = null,
    double DepotLat = 0,
    double DepotLng = 0,
    DateTime? DepartureTime = null);

