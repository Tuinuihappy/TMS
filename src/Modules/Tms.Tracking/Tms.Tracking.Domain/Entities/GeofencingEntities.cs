using Tms.SharedKernel.Domain;
using Tms.Tracking.Domain.Enums;

namespace Tms.Tracking.Domain.Entities;

/// <summary>
/// Geo Zone — Aggregate Root.
/// รองรับทั้ง Circle (RadiusMeters ≠ null) และ Polygon (PolygonCoordinates ≠ null)
/// </summary>
public sealed class GeoZone : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public GeoZoneType Type { get; private set; }
    public Guid? LocationId { get; private set; } // FK to Master Data Location
    /// <summary>Optional: ระบุว่า Zone นี้ใช้สำหรับ Pickup หรือ Dropoff — null = ไม่ระบุ</summary>
    public string? StopType { get; private set; } // "Pickup" | "Dropoff" | null
    public Guid TenantId { get; private set; }
    public bool IsActive { get; private set; }

    // Circle
    public double? CenterLatitude { get; private set; }
    public double? CenterLongitude { get; private set; }
    public double? RadiusMeters { get; private set; }

    // Polygon — stored as JSONB [[lng,lat],[lng,lat],...]
    public string? PolygonCoordinatesJson { get; private set; }

    private GeoZone() { }

    public static GeoZone CreateCircle(
        string name,
        Guid tenantId,
        double centerLat,
        double centerLng,
        double radiusMeters,
        Guid? locationId = null,
        string? stopType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new GeoZone
        {
            Name = name,
            TenantId = tenantId,
            Type = GeoZoneType.Circle,
            LocationId = locationId,
            StopType = stopType,
            CenterLatitude = centerLat,
            CenterLongitude = centerLng,
            RadiusMeters = radiusMeters,
            IsActive = true
        };
    }

    public static GeoZone CreatePolygon(
        string name,
        Guid tenantId,
        string polygonCoordinatesJson,
        Guid? locationId = null,
        string? stopType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new GeoZone
        {
            Name = name,
            TenantId = tenantId,
            Type = GeoZoneType.Polygon,
            LocationId = locationId,
            StopType = stopType,
            PolygonCoordinatesJson = polygonCoordinatesJson,
            IsActive = true
        };
    }

    public void Update(string name, double? centerLat, double? centerLng, double? radiusMeters, string? polygonJson)
    {
        Name = name;
        CenterLatitude = centerLat;
        CenterLongitude = centerLng;
        RadiusMeters = radiusMeters;
        PolygonCoordinatesJson = polygonJson;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Returns true if the given point is inside this zone.
    /// Circle: Haversine distance ≤ RadiusMeters
    /// Polygon: Ray-casting algorithm
    /// </summary>
    public bool CheckPointInside(double lat, double lng)
    {
        return Type switch
        {
            GeoZoneType.Circle => IsInsideCircle(lat, lng),
            GeoZoneType.Polygon => IsInsidePolygon(lat, lng),
            _ => false
        };
    }

    private bool IsInsideCircle(double lat, double lng)
    {
        if (CenterLatitude is null || CenterLongitude is null || RadiusMeters is null)
            return false;

        var distanceM = HaversineDistanceMeters(lat, lng, CenterLatitude.Value, CenterLongitude.Value);
        return distanceM <= RadiusMeters.Value;
    }

    private bool IsInsidePolygon(double lat, double lng)
    {
        if (string.IsNullOrWhiteSpace(PolygonCoordinatesJson))
            return false;

        // Parse [[lng,lat],[lng,lat],...] — simple JSON parsing without library
        var coords = ParsePolygonCoords(PolygonCoordinatesJson);
        return RayCasting(lat, lng, coords);
    }

    // ── Geometry Helpers ─────────────────────────────────────────────────────
    private static double HaversineDistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000; // Earth radius in metres
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    // Ray-casting: point-in-polygon
    private static bool RayCasting(double lat, double lng, List<(double Lng, double Lat)> polygon)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygon[i].Lng; var yi = polygon[i].Lat;
            var xj = polygon[j].Lng; var yj = polygon[j].Lat;
            bool intersect = (yi > lat) != (yj > lat)
                && lng < (xj - xi) * (lat - yi) / (yj - yi) + xi;
            if (intersect) inside = !inside;
        }
        return inside;
    }

    // Very lightweight JSON array parser for [[lng,lat],...] without System.Text.Json dependency
    private static List<(double Lng, double Lat)> ParsePolygonCoords(string json)
    {
        var result = new List<(double, double)>();
        // Remove outer brackets
        var trimmed = json.Trim().TrimStart('[').TrimEnd(']');
        // Split on "],[" 
        var pairStrings = trimmed.Split("],[", StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairStrings)
        {
            var cleaned = pair.TrimStart('[').TrimEnd(']');
            var parts = cleaned.Split(',');
            if (parts.Length >= 2
                && double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lngVal)
                && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var latVal))
            {
                result.Add((lngVal, latVal));
            }
        }
        return result;
    }
}

/// <summary>Zone entry/exit event record</summary>
public sealed class ZoneEvent : BaseEntity
{
    public Guid ZoneId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Guid? TripId { get; private set; }
    public ZoneEventType EventType { get; private set; }
    public DateTime Timestamp { get; private set; }
    public Guid TenantId { get; private set; }

    private ZoneEvent() { }

    public static ZoneEvent Create(
        Guid zoneId,
        Guid vehicleId,
        ZoneEventType eventType,
        Guid tenantId,
        Guid? tripId = null)
    {
        return new ZoneEvent
        {
            ZoneId = zoneId,
            VehicleId = vehicleId,
            TripId = tripId,
            EventType = eventType,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow
        };
    }
}
