using Google.OrTools.ConstraintSolver;
using Microsoft.Extensions.Logging;

namespace Tms.Planning.Application;

/// <summary>
/// ผลลัพธ์จาก OR-Tools VRP Solver — หนึ่งเส้นทาง (Route) ของรถ 1 คัน
/// </summary>
public sealed record VrpRouteResult(
    List<VrpStopResult> Stops,
    decimal TotalDistanceKm,
    int EstimatedDurationMin);

public sealed record VrpStopResult(
    int OriginalNodeIndex,
    Guid OrderId,
    string StopType,   // "Pickup" | "Dropoff"
    double Latitude,
    double Longitude,
    DateTime? EstimatedArrival);

/// <summary>
/// ข้อมูล Order ที่ส่งเข้า VRP Solver — แต่ละ Order มี Pickup + Dropoff
/// </summary>
public sealed record VrpOrderInput(
    Guid OrderId,
    double PickupLat, double PickupLng,
    double DropoffLat, double DropoffLng,
    decimal WeightKg,
    decimal VolumeCbm,
    DateTime? PickupWindowFrom,
    DateTime? PickupWindowTo,
    DateTime? DropoffWindowFrom,
    DateTime? DropoffWindowTo);

/// <summary>
/// Google OR-Tools based Pickup & Delivery VRP Solver
/// <br/>
/// ใช้ RoutingModel ของ Google OR-Tools ในการจัดเส้นทาง
/// รองรับ Constraints:
/// <list type="bullet">
///   <item>Pickup ต้องมาก่อน Dropoff (Precedence)</item>
///   <item>Pickup + Dropoff ต้องอยู่รถคันเดียวกัน</item>
///   <item>Capacity Constraint (น้ำหนัก)</item>
///   <item>Distance minimization</item>
/// </list>
/// </summary>
public sealed class OrToolsVrpSolver
{
    private const double EarthRadiusKm = 6371.0;
    private const double AvgSpeedKmh = 40.0;
    private const int ServiceTimeMin = 15;

    /// <summary>
    /// แก้ปัญหา VRP แบบ Pickup & Delivery ด้วย Google OR-Tools
    /// </summary>
    /// <param name="orders">รายการ Orders พร้อมพิกัด Pickup/Dropoff</param>
    /// <param name="numVehicles">จำนวนรถที่ใช้ได้</param>
    /// <param name="vehicleCapacityKg">ขีดจำกัดน้ำหนักต่อคัน (kg)</param>
    /// <param name="maxDistancePerVehicleKm">ระยะทางสูงสุดต่อคัน (km)</param>
    /// <param name="timeLimitSeconds">เวลาที่ให้ Solver คำนวณ (วินาที)</param>
    /// <param name="depotLat">ละติจูดจุดเริ่มต้น (Depot)</param>
    /// <param name="depotLng">ลองจิจูดจุดเริ่มต้น (Depot)</param>
    /// <param name="departureTime">เวลาออกจาก Depot</param>
    /// <param name="logger">Logger</param>
    /// <returns>รายการ Routes (1 route = 1 รถ) เฉพาะที่มี stops</returns>
    public List<VrpRouteResult> Solve(
        List<VrpOrderInput> orders,
        int numVehicles = 4,
        decimal vehicleCapacityKg = 5000m,
        decimal maxDistancePerVehicleKm = 300m,
        int timeLimitSeconds = 10,
        double depotLat = 13.7563,
        double depotLng = 100.5018,
        DateTime? departureTime = null,
        ILogger? logger = null)
    {
        if (orders.Count == 0) return [];

        var departure = departureTime ?? DateTime.UtcNow;

        // ── 1. สร้าง Node List ──────────────────────────────────────────
        // Node 0 = Depot
        // Node 1,2 = Pickup/Dropoff ของ Order 0
        // Node 3,4 = Pickup/Dropoff ของ Order 1 ...
        var nodes = new List<(double Lat, double Lng)> { (depotLat, depotLng) };
        var pickupDeliveryPairs = new List<int[]>();
        var demands = new List<long> { 0 }; // depot demand = 0

        for (int i = 0; i < orders.Count; i++)
        {
            var o = orders[i];
            var pickupIdx = nodes.Count;
            nodes.Add((o.PickupLat, o.PickupLng));
            demands.Add((long)o.WeightKg); // pickup adds weight

            var dropoffIdx = nodes.Count;
            nodes.Add((o.DropoffLat, o.DropoffLng));
            demands.Add(-(long)o.WeightKg); // dropoff removes weight

            pickupDeliveryPairs.Add([pickupIdx, dropoffIdx]);
        }

        int nodeCount = nodes.Count;

        // ── 2. สร้าง Distance Matrix (Haversine, หน่วย: เมตร) ────────
        var distMatrix = new long[nodeCount, nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            for (int j = 0; j < nodeCount; j++)
            {
                if (i == j) { distMatrix[i, j] = 0; continue; }
                var km = HaversineKm(nodes[i].Lat, nodes[i].Lng, nodes[j].Lat, nodes[j].Lng);
                distMatrix[i, j] = (long)(km * 1000); // แปลงเป็นเมตร
            }
        }

        // ── 3. สร้าง OR-Tools Routing Model ─────────────────────────────
        var manager = new RoutingIndexManager(nodeCount, numVehicles, 0);
        var routing = new RoutingModel(manager);

        // Distance callback
        int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            var toNode = manager.IndexToNode(toIndex);
            return distMatrix[fromNode, toNode];
        });
        routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

        // ── 4. Distance Dimension ───────────────────────────────────────
        long maxDistMeters = (long)(maxDistancePerVehicleKm * 1000);
        routing.AddDimension(
            transitCallbackIndex,
            0,               // no slack
            maxDistMeters,   // max distance per vehicle
            true,            // start cumul to zero
            "Distance");
        var distanceDimension = routing.GetMutableDimension("Distance");
        distanceDimension.SetGlobalSpanCostCoefficient(100);

        // ── 5. Capacity Dimension ───────────────────────────────────────
        int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
        {
            var fromNode = manager.IndexToNode(fromIndex);
            return demands[fromNode];
        });

        long[] vehicleCapacities = new long[numVehicles];
        Array.Fill(vehicleCapacities, (long)vehicleCapacityKg);

        routing.AddDimensionWithVehicleCapacity(
            demandCallbackIndex,
            0,                  // no slack
            vehicleCapacities,  // vehicle max capacities
            true,               // start cumul to zero
            "Capacity");

        // ── 6. Pickup & Delivery Constraints ────────────────────────────
        var solver = routing.solver();
        foreach (var pair in pickupDeliveryPairs)
        {
            long pickupIndex = manager.NodeToIndex(pair[0]);
            long deliveryIndex = manager.NodeToIndex(pair[1]);
            routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);

            // Same vehicle constraint
            solver.Add(solver.MakeEquality(
                routing.VehicleVar(pickupIndex),
                routing.VehicleVar(deliveryIndex)));

            // Precedence: Pickup before Delivery
            solver.Add(solver.MakeLessOrEqual(
                distanceDimension.CumulVar(pickupIndex),
                distanceDimension.CumulVar(deliveryIndex)));
        }

        // ── 7. Search Parameters ────────────────────────────────────────
        var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
        searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
        searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
        searchParameters.TimeLimit = new Google.Protobuf.WellKnownTypes.Duration { Seconds = timeLimitSeconds };

        logger?.LogInformation("OR-Tools VRP: {Nodes} nodes, {Vehicles} vehicles, {Orders} orders. Solving...",
            nodeCount, numVehicles, orders.Count);

        // ── 8. Solve ────────────────────────────────────────────────────
        var solution = routing.SolveWithParameters(searchParameters);

        if (solution == null)
        {
            logger?.LogWarning("OR-Tools VRP: No solution found. Falling back to greedy.");
            return FallbackGreedy(orders, depotLat, depotLng, departure);
        }

        // ── 9. Extract Routes ───────────────────────────────────────────
        var results = new List<VrpRouteResult>();

        for (int v = 0; v < numVehicles; v++)
        {
            if (!routing.IsVehicleUsed(solution, v)) continue;

            var stops = new List<VrpStopResult>();
            long totalDistMeters = 0;
            var currentTime = departure;
            var index = routing.Start(v);

            while (!routing.IsEnd(index))
            {
                var prevIndex = index;
                index = solution.Value(routing.NextVar(index));
                totalDistMeters += routing.GetArcCostForVehicle(prevIndex, index, v);

                var nodeIdx = manager.IndexToNode(index);
                if (nodeIdx == 0 || routing.IsEnd(index)) continue; // skip depot

                // Map node back to order
                var orderIndex = (nodeIdx - 1) / 2;
                var isPickup = (nodeIdx - 1) % 2 == 0;
                var order = orders[orderIndex];

                var distKm = HaversineKm(
                    nodeIdx > 0 ? nodes[manager.IndexToNode(prevIndex)].Lat : depotLat,
                    nodeIdx > 0 ? nodes[manager.IndexToNode(prevIndex)].Lng : depotLng,
                    nodes[nodeIdx].Lat, nodes[nodeIdx].Lng);
                currentTime = currentTime.AddMinutes(distKm / AvgSpeedKmh * 60 + ServiceTimeMin);

                stops.Add(new VrpStopResult(
                    OriginalNodeIndex: nodeIdx,
                    OrderId: order.OrderId,
                    StopType: isPickup ? "Pickup" : "Dropoff",
                    Latitude: nodes[nodeIdx].Lat,
                    Longitude: nodes[nodeIdx].Lng,
                    EstimatedArrival: currentTime
                ));
            }

            if (stops.Count > 0)
            {
                var distKm = Math.Round((decimal)totalDistMeters / 1000m, 2);
                var durationMin = (int)Math.Ceiling((double)distKm / AvgSpeedKmh * 60 + stops.Count * ServiceTimeMin);
                results.Add(new VrpRouteResult(stops, distKm, durationMin));
            }
        }

        logger?.LogInformation("OR-Tools VRP: Found {RouteCount} routes, Objective={Obj}",
            results.Count, solution.ObjectiveValue());

        return results;
    }

    /// <summary>
    /// Fallback: ถ้า OR-Tools แก้ไม่ได้ ให้ใช้ greedy simple route
    /// </summary>
    private static List<VrpRouteResult> FallbackGreedy(
        List<VrpOrderInput> orders, double depotLat, double depotLng, DateTime departure)
    {
        var stops = new List<VrpStopResult>();
        int seq = 0;
        decimal totalDist = 0;
        double lastLat = depotLat, lastLng = depotLng;
        var currentTime = departure;

        foreach (var o in orders)
        {
            var d1 = HaversineKm(lastLat, lastLng, o.PickupLat, o.PickupLng);
            currentTime = currentTime.AddMinutes(d1 / AvgSpeedKmh * 60 + ServiceTimeMin);
            stops.Add(new VrpStopResult(seq++, o.OrderId, "Pickup", o.PickupLat, o.PickupLng, currentTime));

            var d2 = HaversineKm(o.PickupLat, o.PickupLng, o.DropoffLat, o.DropoffLng);
            currentTime = currentTime.AddMinutes(d2 / AvgSpeedKmh * 60 + ServiceTimeMin);
            stops.Add(new VrpStopResult(seq++, o.OrderId, "Dropoff", o.DropoffLat, o.DropoffLng, currentTime));

            totalDist += (decimal)(d1 + d2);
            lastLat = o.DropoffLat;
            lastLng = o.DropoffLng;
        }

        return [new VrpRouteResult(stops, Math.Round(totalDist, 2),
            (int)Math.Ceiling((double)totalDist / AvgSpeedKmh * 60 + stops.Count * ServiceTimeMin))];
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
