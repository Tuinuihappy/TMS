using Tms.SharedKernel.Domain;

namespace Tms.Tracking.Domain.Entities;

/// <summary>GPS position record (time-series, append-only)</summary>
public sealed class VehiclePosition : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public Guid? TripId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public decimal SpeedKmh { get; private set; }
    public decimal Heading { get; private set; }
    public bool IsEngineOn { get; private set; }
    public DateTime Timestamp { get; private set; }

    private VehiclePosition() { }

    public static VehiclePosition Create(
        Guid vehicleId,
        double latitude,
        double longitude,
        decimal speedKmh,
        decimal heading,
        bool isEngineOn,
        DateTime timestamp,
        Guid? tripId = null)
    {
        return new VehiclePosition
        {
            VehicleId = vehicleId,
            TripId = tripId,
            Latitude = latitude,
            Longitude = longitude,
            SpeedKmh = speedKmh,
            Heading = heading,
            IsEngineOn = isEngineOn,
            Timestamp = timestamp
        };
    }
}

/// <summary>Latest position cache (UPSERT target — one row per vehicle)</summary>
public sealed class CurrentVehicleState
{
    public Guid VehicleId { get; private set; }
    public Guid TenantId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public decimal SpeedKmh { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    private CurrentVehicleState() { }

    public static CurrentVehicleState Create(
        Guid vehicleId,
        Guid tenantId,
        double latitude,
        double longitude,
        decimal speedKmh,
        DateTime timestamp)
    {
        return new CurrentVehicleState
        {
            VehicleId = vehicleId,
            TenantId = tenantId,
            Latitude = latitude,
            Longitude = longitude,
            SpeedKmh = speedKmh,
            LastUpdatedAt = timestamp
        };
    }

    public void Update(double latitude, double longitude, decimal speedKmh, DateTime timestamp)
    {
        Latitude = latitude;
        Longitude = longitude;
        SpeedKmh = speedKmh;
        LastUpdatedAt = timestamp;
    }
}
