using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tms.Planning.Application.Common.Interfaces;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Enums;

namespace Tms.Planning.Application.Features.AutoPlanning;

public sealed class ProcessOptimizationRequestCommandHandler(
    IPlanningDbContext dbContext,
    OrToolsVrpSolver vrpSolver,
    ILogger<ProcessOptimizationRequestCommandHandler> logger) : IRequestHandler<ProcessOptimizationRequestCommand>
{
    public async Task Handle(ProcessOptimizationRequestCommand request, CancellationToken cancellationToken)
    {
        // 1. ดึง Optimization Request ออกมา
        var optRequest = await dbContext.OptimizationRequests
            .FirstOrDefaultAsync(r => r.Id == request.OptimizationRequestId, cancellationToken);

        if (optRequest == null || optRequest.Status != OptimizationStatus.Pending)
        {
            logger.LogWarning("OptimizationRequest {Id} not found or not in Pending state.", request.OptimizationRequestId);
            return;
        }

        // Marking as Processing
        optRequest.MarkProcessing();
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Processing Optimization Request {Id}. Invoking Google OR-Tools VRP Engine...", optRequest.Id);

        try
        {
            // 2. Parse Order Ids จาก Parameters JSON
            var paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(optRequest.ParametersJson ?? "{}");
            var orderIdElements = paramDict?["OrderIds"] as JsonElement?;
            var orderIds = orderIdElements?.EnumerateArray().Select(x => x.GetGuid()).ToList() ?? new List<Guid>();

            if (orderIds.Count == 0)
                throw new Exception("No orders provided in Optimization Request parameters.");

            // 3. ดึงข้อมูล PlanningOrders ที่มี Constraints จริง
            var planningOrders = await dbContext.PlanningOrders
                .Where(o => orderIds.Contains(o.OrderId))
                .ToListAsync(cancellationToken);

            // 4. แปลงเป็น VRP Input
            var vrpInputs = planningOrders.Select(po => new VrpOrderInput(
                OrderId: po.OrderId,
                PickupLat: po.PickupLatitude,
                PickupLng: po.PickupLongitude,
                DropoffLat: po.DropoffLatitude,
                DropoffLng: po.DropoffLongitude,
                WeightKg: po.TotalWeight,
                VolumeCbm: po.TotalVolume,
                PickupWindowFrom: po.ReadyTime,
                PickupWindowTo: po.ReadyTime?.AddHours(2),
                DropoffWindowFrom: po.DueTime?.AddHours(-2),
                DropoffWindowTo: po.DueTime
            )).ToList();

            // 5. เรียก Google OR-Tools VRP Solver
            var vrpRoutes = vrpSolver.Solve(
                orders: vrpInputs,
                numVehicles: Math.Max(1, (int)Math.Ceiling(vrpInputs.Count / 10.0)),
                vehicleCapacityKg: 5000m,
                maxDistancePerVehicleKm: 500m,
                timeLimitSeconds: 10,
                logger: logger
            );

            logger.LogInformation("OR-Tools returned {RouteCount} routes for {OrderCount} orders.",
                vrpRoutes.Count, vrpInputs.Count);

            // 6. สร้าง RoutePlan + Trip สำหรับแต่ละ Route
            var planDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var allPlans = new List<RoutePlan>();

            foreach (var (route, routeIdx) in vrpRoutes.Select((r, i) => (r, i)))
            {
                var routePlan = RoutePlan.Create(
                    planNumber: $"RP-{DateTime.UtcNow:yyyyMMdd}-{optRequest.Id.ToString()[..4].ToUpper()}-V{routeIdx}",
                    plannedDate: planDate,
                    tenantId: optRequest.TenantId,
                    vehicleTypeId: null
                );

                // เพิ่ม Stops ตามลำดับที่ OR-Tools จัดให้
                int seq = 1;
                foreach (var stop in route.Stops)
                {
                    routePlan.AddStop(RouteStop.Create(
                        routePlanId: routePlan.Id,
                        sequence: seq++,
                        orderId: stop.OrderId,
                        stopType: stop.StopType,
                        lat: stop.Latitude,
                        lng: stop.Longitude,
                        etaArrival: stop.EstimatedArrival
                    ));
                }

                // อัปเดต Metrics จาก OR-Tools 
                var capacityPct = vrpInputs.Count > 0
                    ? Math.Round(route.Stops.Where(s => s.StopType == "Pickup")
                        .Sum(s => vrpInputs.FirstOrDefault(o => o.OrderId == s.OrderId)?.WeightKg ?? 0) / 5000m * 100, 1)
                    : 0m;
                routePlan.UpdateMetrics(route.TotalDistanceKm, route.EstimatedDurationMin, capacityPct);

                dbContext.RoutePlans.Add(routePlan);
                allPlans.Add(routePlan);

                logger.LogInformation("Created RoutePlan {PlanNumber}: {StopCount} stops, {DistKm} km",
                    routePlan.PlanNumber, routePlan.Stops.Count, route.TotalDistanceKm);
            }

            // 7. Complete Optimization Request
            var resultJson = JsonSerializer.Serialize(new
            {
                RoutePlanIds = allPlans.Select(p => p.Id).ToList(),
                TotalRoutes = allPlans.Count,
                Solver = "Google OR-Tools",
                Message = "VRP Completed Successfully"
            });
            optRequest.Complete(resultJson, allPlans);

            // 8. ปล่อย Lock ให้ PlanningOrders → Planned
            foreach (var po in planningOrders)
            {
                po.MarkAsPlanned();
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OptimizationRequest {Id} Completed. Created {Count} RoutePlan(s) via OR-Tools.",
                optRequest.Id, allPlans.Count);

            // 9. Auto-Lock RoutePlans + สร้าง Trips
            foreach (var routePlan in allPlans)
            {
                routePlan.Lock();

                var tripNumber = $"TRP-{DateTime.UtcNow:yyyyMMdd}-{routePlan.Id.ToString()[..4].ToUpper()}";
                var newTrip = Trip.Create(
                    tripNumber: tripNumber,
                    plannedDate: routePlan.PlannedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    tenantId: routePlan.TenantId
                );

                foreach (var stop in routePlan.Stops.OrderBy(s => s.Sequence))
                {
                    var stopType = stop.StopType == "Pickup" ? StopType.Pickup : StopType.Dropoff;
                    newTrip.AddStop(
                        sequence: stop.Sequence,
                        orderId: stop.OrderId,
                        type: stopType,
                        lat: stop.Latitude,
                        lng: stop.Longitude,
                        windowFrom: stop.EstimatedArrivalTime,
                        windowTo: stop.EstimatedArrivalTime?.AddHours(1)
                    );
                }

                dbContext.Trips.Add(newTrip);
                logger.LogInformation("Auto-created Trip {TripNumber} with {Count} stops from RoutePlan {PlanNumber}.",
                    newTrip.TripNumber, newTrip.Stops.Count, routePlan.PlanNumber);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process Optimization Request {Id}.", optRequest.Id);
            optRequest.Fail(ex.Message);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
