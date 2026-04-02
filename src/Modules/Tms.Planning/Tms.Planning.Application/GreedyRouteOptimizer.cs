using Tms.Planning.Application.Features;

namespace Tms.Planning.Application;

/// <summary>
/// Greedy Nearest-Neighbor VRP Optimizer
/// 
/// Algorithm:
/// 1. เริ่มจากจุด depot (0,0) หรือ centroid ของ orders ทั้งหมด
/// 2. วนซ้ำ: เลือก order ที่ยังไม่ได้รับการจัดสรร และอยู่ใกล้ที่สุดจากจุดปัจจุบัน
/// 3. เมื่อเต็ม max stops ให้สร้าง route ใหม่
///
/// Time complexity: O(n²) — เหมาะสำหรับ n < 200 orders
/// </summary>
public sealed class GreedyRouteOptimizer
{
    public List<List<OrderLocationInput>> Optimize(
        List<OrderLocationInput> orders,
        int maxStopsPerRoute)
    {
        if (orders.Count == 0) return [];

        var unvisited = new HashSet<OrderLocationInput>(orders);
        var routes = new List<List<OrderLocationInput>>();

        while (unvisited.Count > 0)
        {
            var route = new List<OrderLocationInput>();
            var current = (Lat: 0.0, Lng: 0.0); // depot (could be warehouse location)

            while (route.Count < maxStopsPerRoute && unvisited.Count > 0)
            {
                var nearest = FindNearest(current.Lat, current.Lng, unvisited);
                route.Add(nearest);
                unvisited.Remove(nearest);
                current = (nearest.Lat, nearest.Lng);
            }

            routes.Add(route);
        }

        return routes;
    }

    private static OrderLocationInput FindNearest(
        double fromLat, double fromLng,
        IEnumerable<OrderLocationInput> candidates)
    {
        return candidates
            .MinBy(o => HaversineDistanceKm(fromLat, fromLng, o.Lat, o.Lng))!;
    }

    private static double HaversineDistanceKm(double lat1, double lng1, double lat2, double lng2)
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
}
