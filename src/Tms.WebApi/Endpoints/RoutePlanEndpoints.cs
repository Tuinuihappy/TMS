using MediatR;
using Tms.Planning.Application.Features;

namespace Tms.WebApi.Endpoints;

public static class RoutePlanEndpoints
{
    public static IEndpointRouteBuilder MapRoutePlanEndpoints(this IEndpointRouteBuilder app)
    {
        var optimize = app.MapGroup("/api/planning/optimize").WithTags("RoutePlanning");
        var plans = app.MapGroup("/api/planning/plans").WithTags("RoutePlanning");

        // POST /api/planning/optimize
        optimize.MapPost("/", async (
            OptimizeRequest req, ISender sender, CancellationToken ct) =>
        {
            var orders = req.Orders.Select(o =>
                new OrderLocationInput(o.OrderId, o.Lat, o.Lng, o.WeightKg)).ToList();

            var requestId = await sender.Send(new RequestOptimizationCommand(
                orders, req.VehicleTypeId,
                req.PlannedDate, req.TenantId, req.MaxStopsPerRoute), ct);

            return Results.Accepted($"/api/planning/optimize/{requestId}",
                new { OptimizationRequestId = requestId, Status = "Pending" });
        })
        .WithName("RequestRouteOptimization")
        .WithSummary("ส่งคำขอ Route Optimization (VRP)");

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

        return app;
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record OrderLocationRequest(
    Guid OrderId, double Lat, double Lng, decimal WeightKg = 0);

public sealed record OptimizeRequest(
    List<OrderLocationRequest> Orders,
    Guid TenantId,
    DateOnly PlannedDate,
    Guid? VehicleTypeId = null,
    int MaxStopsPerRoute = 20);

public sealed record ReorderStopRequest(Guid StopId, int NewSequence);
public sealed record ReorderStopsRequest(List<ReorderStopRequest> Reorder);
