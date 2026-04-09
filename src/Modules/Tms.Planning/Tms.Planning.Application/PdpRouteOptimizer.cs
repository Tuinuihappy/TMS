using Tms.Planning.Application.Features;

namespace Tms.Planning.Application;

// ── PDP Stop (output node) ───────────────────────────────────────────────────

/// <summary>
/// หนึ่ง stop ใน PDP route — มี StopType ระบุว่าเป็น Pickup หรือ Dropoff
/// </summary>
public sealed record PdpStop(
    Guid OrderId,
    string StopType,   // "Pickup" | "Dropoff"
    double Lat,
    double Lng);

// ── PDP Route Optimizer ──────────────────────────────────────────────────────

/// <summary>
/// Pickup-and-Delivery Problem (PDP) Route Optimizer
/// <br/>
/// Algorithm: Nearest Neighbor with Precedence Constraints
/// <br/>
/// กฎหลัก:
/// <list type="bullet">
///   <item>Pickup ของ Order X ต้องมาก่อน Dropoff ของ Order X เสมอ (Precedence)</item>
///   <item>Pickup และ Dropoff ของ Order X ต้องอยู่ใน Route เดียวกัน</item>
///   <item>Load บนรถต้องไม่เกิน maxCapacityKg ณ เวลาใดเวลาหนึ่ง</item>
/// </list>
/// <br/>
/// Time complexity: O(n²) — เหมาะสำหรับ n &lt; 200 orders
/// </summary>
public sealed class PdpRouteOptimizer
{
    /// <summary>
    /// Optimize orders into routes, each route is a sequence of PdpStop (Pickup/Dropoff).
    /// </summary>
    /// <param name="orders">Input orders — each has Pickup and Dropoff location</param>
    /// <param name="maxOrdersPerRoute">จำนวน Orders สูงสุดต่อ Route (= maxStops / 2)</param>
    /// <param name="maxCapacityKg">น้ำหนักสูงสุดที่รถรับได้</param>
    /// <param name="maxCapacityVolumeCBM">ปริมาตรสูงสุดที่รถรับได้ (CBM) — 0 = ไม่จำกัด</param>
    /// <param name="depotLat">ละติจูดของจุดเริ่มต้น (คลังสินค้า)</param>
    /// <param name="depotLng">ลองจิจูดของจุดเริ่มต้น</param>
    /// <param name="departureTime">เวลาออกจาก depot — ใช้สำหรับ time-window check</param>
    public List<List<PdpStop>> Optimize(
        List<PdpOrderInput> orders,
        int maxOrdersPerRoute = 10,
        decimal maxCapacityKg = 10_000m,
        decimal maxCapacityVolumeCBM = 0m,      // 0 = unconstrained
        double depotLat = 0,
        double depotLng = 0,
        DateTime? departureTime = null)
    {
        if (orders.Count == 0) return [];

        var unassigned = new HashSet<Guid>(orders.Select(o => o.OrderId));
        var orderMap = orders.ToDictionary(o => o.OrderId);
        var routes = new List<List<PdpStop>>();

        while (unassigned.Count > 0)
        {
            var route = BuildRoute(
                unassigned, orderMap,
                maxOrdersPerRoute, maxCapacityKg, maxCapacityVolumeCBM,
                depotLat, depotLng,
                departureTime ?? DateTime.UtcNow);

            routes.Add(route);
        }

        return routes;
    }


    // ── Core route building ──────────────────────────────────────────────────

    private static List<PdpStop> BuildRoute(
        HashSet<Guid> unassigned,
        Dictionary<Guid, PdpOrderInput> orderMap,
        int maxOrdersPerRoute,
        decimal maxCapacityKg,
        decimal maxCapacityVolumeCBM,
        double depotLat,
        double depotLng,
        DateTime currentTime)
    {
        var route          = new List<PdpStop>();
        var pendingPickup  = new HashSet<Guid>();
        var pendingDropoff = new HashSet<Guid>();

        var currentLat           = depotLat;
        var currentLng           = depotLng;
        var currentLoadKg        = 0m;
        var currentLoadVolumeCBM = 0m;
        var assignedOrdersCount  = 0;
        const double avgSpeedKmh = 40.0;

        while (true)
        {
            var canAddNewOrder = assignedOrdersCount < maxOrdersPerRoute
                              && unassigned.Count > 0;

            // Available pickup nodes — must satisfy BOTH weight AND volume constraints
            var availablePickups = canAddNewOrder
                ? unassigned
                    .Where(id =>
                    {
                        var o = orderMap[id];
                        bool weightOk = currentLoadKg + o.WeightKg <= maxCapacityKg;
                        bool volumeOk = maxCapacityVolumeCBM <= 0
                                     || (currentLoadVolumeCBM + o.VolumeCBM) <= maxCapacityVolumeCBM;
                        return weightOk && volumeOk;
                    })
                    .Select(id => (Id: id, IsPickup: true,
                                   Lat: orderMap[id].PickupLat,
                                   Lng: orderMap[id].PickupLng))
                    .ToList()
                : new List<(Guid Id, bool IsPickup, double Lat, double Lng)>();

            var availableDropoffs = pendingDropoff
                .Select(id => (Id: id, IsPickup: false,
                               Lat: orderMap[id].DropoffLat,
                               Lng: orderMap[id].DropoffLng))
                .ToList();

            var candidates = availablePickups.Concat(availableDropoffs).ToList();
            if (candidates.Count == 0) break;

            // ── Choose nearest with time-window soft penalty ─────────────────
            var nearest = candidates
                .MinBy(c =>
                {
                    var distKm = HaversineKm(currentLat, currentLng, c.Lat, c.Lng);
                    var etaMinutes = distKm / avgSpeedKmh * 60.0;
                    var eta = currentTime.AddMinutes(etaMinutes);

                    // Time-window soft constraint
                    var order = orderMap[c.Id];
                    double penalty = 1.0;

                    if (c.IsPickup && order.PickupWindowTo.HasValue && eta > order.PickupWindowTo.Value)
                        penalty = 1.5;   // Soft: penalize late pickup (don't discard)
                    else if (!c.IsPickup && order.DropoffWindowTo.HasValue && eta > order.DropoffWindowTo.Value)
                        penalty = 1.5;   // Soft: penalize late delivery
                    else if (c.IsPickup && order.PickupWindowFrom.HasValue && eta < order.PickupWindowFrom.Value)
                        penalty = 0.9;   // Slight preference: early pickup that fits window

                    return distKm * penalty;
                })!;

            // ── Update travel time ──────────────────────────────────────────
            var travelKm  = HaversineKm(currentLat, currentLng, nearest.Lat, nearest.Lng);
            currentTime   = currentTime.AddMinutes(travelKm / avgSpeedKmh * 60.0 + 15); // +15 service time

            // ── Apply choice ─────────────────────────────────────────────────
            if (nearest.IsPickup)
            {
                var orderId = nearest.Id;
                route.Add(new PdpStop(orderId, "Pickup", nearest.Lat, nearest.Lng));
                unassigned.Remove(orderId);
                pendingDropoff.Add(orderId);
                currentLoadKg        += orderMap[orderId].WeightKg;
                currentLoadVolumeCBM += orderMap[orderId].VolumeCBM;
                assignedOrdersCount++;
            }
            else
            {
                var orderId = nearest.Id;
                route.Add(new PdpStop(orderId, "Dropoff", nearest.Lat, nearest.Lng));
                pendingDropoff.Remove(orderId);
                currentLoadKg        -= orderMap[orderId].WeightKg;
                currentLoadVolumeCBM -= orderMap[orderId].VolumeCBM;
            }

            currentLat = nearest.Lat;
            currentLng = nearest.Lng;

            if (assignedOrdersCount >= maxOrdersPerRoute && pendingDropoff.Count == 0)
                break;
        }

        return route;
    }


    // ── Distance ─────────────────────────────────────────────────────────────

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    // ── Metrics helper ────────────────────────────────────────────────────────

    /// <summary>
    /// คำนวณระยะทางรวมของ route (km) สำหรับบันทึกไว้ใน RoutePlan
    /// </summary>
    public static decimal ComputeTotalDistanceKm(
        IReadOnlyList<PdpStop> route,
        double depotLat = 0, double depotLng = 0)
    {
        if (route.Count == 0) return 0;
        double total = HaversineKm(depotLat, depotLng, route[0].Lat, route[0].Lng);
        for (int i = 1; i < route.Count; i++)
            total += HaversineKm(route[i - 1].Lat, route[i - 1].Lng,
                                  route[i].Lat, route[i].Lng);
        return Math.Round((decimal)total, 2);
    }

    /// <summary>
    /// ประมาณเวลาขับ (นาที) โดยสมมติ avgSpeedKmh = 40 km/h
    /// </summary>
    public static int EstimateDurationMin(decimal distanceKm, double avgSpeedKmh = 40)
        => (int)Math.Ceiling((double)distanceKm / avgSpeedKmh * 60);

    /// <summary>
    /// คำนวณ capacity utilization (%) สำหรับ route
    /// </summary>
    public static decimal ComputeCapacityUtil(
        IReadOnlyList<PdpStop> route,
        Dictionary<Guid, PdpOrderInput> orderMap,
        decimal maxCapacityKg)
    {
        if (maxCapacityKg == 0) return 0;
        var totalWeight = route
            .Where(s => s.StopType == "Pickup")
            .Sum(s => orderMap.TryGetValue(s.OrderId, out var o) ? o.WeightKg : 0);
        return maxCapacityKg == 0 ? 0 : Math.Round(totalWeight / maxCapacityKg * 100, 1);
    }
}
