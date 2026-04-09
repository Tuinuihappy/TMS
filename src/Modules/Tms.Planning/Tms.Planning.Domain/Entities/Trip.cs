using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Planning.Domain.Entities;

public enum TripStatus { Created, Assigned, Dispatched, InProgress, Completed, Cancelled }
public enum StopType { Pickup, Dropoff, Return }
public enum StopStatus { Pending, Arrived, Completed, Skipped }

public sealed class Stop : BaseEntity
{
    public Guid TripId { get; private set; }
    public int Sequence { get; private set; }
    public Guid OrderId { get; private set; }
    public StopType Type { get; private set; }
    public StopStatus Status { get; private set; }
    // Address snapshot
    public string? AddressName { get; private set; }
    public string? AddressStreet { get; private set; }
    public string? AddressProvince { get; private set; }
    public double? AddressLatitude { get; private set; }
    public double? AddressLongitude { get; private set; }
    // Time window
    public DateTime? WindowFrom { get; private set; }
    public DateTime? WindowTo { get; private set; }
    public DateTime? ArrivalAt { get; private set; }
    public DateTime? DepartureAt { get; private set; }

    private Stop() { }

    public static Stop Create(
        Guid tripId, int sequence, Guid orderId, StopType type,
        string? addressName = null, string? addressStreet = null,
        string? addressProvince = null, double? lat = null, double? lng = null,
        DateTime? windowFrom = null, DateTime? windowTo = null) =>
        new()
        {
            TripId = tripId, Sequence = sequence, OrderId = orderId,
            Type = type, Status = StopStatus.Pending,
            AddressName = addressName, AddressStreet = addressStreet,
            AddressProvince = addressProvince,
            AddressLatitude = lat, AddressLongitude = lng,
            WindowFrom = windowFrom, WindowTo = windowTo
        };

    public void Arrive() { Status = StopStatus.Arrived; ArrivalAt = DateTime.UtcNow; }
    public void Complete() { Status = StopStatus.Completed; DepartureAt = DateTime.UtcNow; }
    public void Skip() => Status = StopStatus.Skipped;
    public void UpdateSequence(int newSequence) => Sequence = newSequence;
}

public sealed class Trip : AggregateRoot
{
    public string TripNumber { get; private set; } = string.Empty;
    public TripStatus Status { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? DriverId { get; private set; }
    public DateTime PlannedDate { get; private set; }
    public decimal TotalWeight { get; private set; }
    public decimal TotalVolumeCBM { get; private set; }
    public decimal? TotalDistanceKm { get; private set; }
    public int? EstimatedDurationMin { get; private set; }
    public string? CancelReason { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<Stop> _stops = [];
    public IReadOnlyCollection<Stop> Stops => _stops.AsReadOnly();

    private Trip() { }

    public static Trip Create(
        string tripNumber, DateTime plannedDate, Guid tenantId,
        decimal totalWeight = 0, decimal totalVolumeCBM = 0,
        Guid? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripNumber);
        return new Trip
        {
            TripNumber = tripNumber, PlannedDate = plannedDate,
            TenantId = tenantId, TotalWeight = totalWeight,
            TotalVolumeCBM = totalVolumeCBM, CreatedBy = createdBy,
            Status = TripStatus.Created, CreatedAt = DateTime.UtcNow
        };
    }

    public Stop AddStop(
        int sequence, Guid orderId, StopType type,
        string? addressName = null, string? addressStreet = null,
        string? addressProvince = null, double? lat = null, double? lng = null,
        DateTime? windowFrom = null, DateTime? windowTo = null)
    {
        if (Status is not (TripStatus.Created or TripStatus.Assigned))
            throw new DomainException("Cannot add stops to trip in current status.", "INVALID_TRIP_STATE");

        var stop = Stop.Create(
            Id, sequence, orderId, type,
            addressName, addressStreet, addressProvince, lat, lng,
            windowFrom, windowTo);
        _stops.Add(stop);
        return stop;
    }

    /// <summary>Rule 1: Trip ต้องมี Stops ≥ 2</summary>
    public void EnsureMinimumStops()
    {
        if (_stops.Count < 2)
            throw new DomainException("Trip must have at least 2 stops (1 Pickup + 1 Dropoff).", "TRIP_INSUFFICIENT_STOPS");
        if (!_stops.Any(s => s.Type == StopType.Pickup))
            throw new DomainException("Trip must have at least 1 Pickup stop.", "TRIP_NO_PICKUP");
        if (!_stops.Any(s => s.Type == StopType.Dropoff))
            throw new DomainException("Trip must have at least 1 Dropoff stop.", "TRIP_NO_DROPOFF");
    }

    /// <summary>Rule 4+7: Assign ได้เมื่อ Vehicle Available และ Driver ไม่ Suspended</summary>
    public void AssignResources(Guid vehicleId, Guid driverId)
    {
        if (Status is not (TripStatus.Created or TripStatus.Assigned))
            throw new DomainException("Cannot assign resources in current trip status.", "INVALID_TRIP_STATE");

        EnsureMinimumStops();
        VehicleId = vehicleId;
        DriverId = driverId;
        Status = TripStatus.Assigned;
    }

    public void Unassign()
    {
        if (Status != TripStatus.Assigned)
            throw new DomainException("Can only unassign an Assigned trip.", "INVALID_TRIP_STATE");
        VehicleId = null; DriverId = null;
        Status = TripStatus.Created;
    }

    /// <summary>Rule 4: Dispatch ได้เฉพาะ Status = Assigned</summary>
    public void Dispatch()
    {
        if (Status != TripStatus.Assigned)
            throw new DomainException("Only Assigned trips can be dispatched.", "INVALID_TRIP_STATE");

        Status = TripStatus.Dispatched;
        DispatchedAt = DateTime.UtcNow;
    }

    public void StartProgress()
    {
        if (Status != TripStatus.Dispatched)
            throw new DomainException("Trip must be Dispatched before starting.", "INVALID_TRIP_STATE");
        Status = TripStatus.InProgress;
    }

    public void Complete()
    {
        if (Status is not (TripStatus.Dispatched or TripStatus.InProgress))
            throw new DomainException("Only Dispatched or InProgress trips can be completed.", "INVALID_TRIP_STATE");
        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new DomainException($"Cannot cancel trip with status '{Status}'.", "INVALID_TRIP_STATE");
        CancelReason = reason;
        Status = TripStatus.Cancelled;
    }

    public void UpdateMetrics(
        decimal totalWeight, decimal totalVolumeCBM,
        decimal? totalDistanceKm = null, int? estimatedDurationMin = null)
    {
        TotalWeight = totalWeight;
        TotalVolumeCBM = totalVolumeCBM;
        TotalDistanceKm = totalDistanceKm;
        EstimatedDurationMin = estimatedDurationMin;
    }

    /// <summary>
    /// Called after each stop completes. Automatically transitions the Trip to Completed
    /// if every Dropoff stop is either Completed or Skipped.
    /// Returns true if the Trip was just auto-completed (caller should stage TripCompletedIntegrationEvent).
    /// </summary>
    public bool TryAutoComplete()
    {
        if (Status is not (TripStatus.Dispatched or TripStatus.InProgress))
            return false;

        var allDropoffsFinished = _stops
            .Where(s => s.Type == StopType.Dropoff)
            .All(s => s.Status is StopStatus.Completed or StopStatus.Skipped);

        if (!allDropoffsFinished) return false;

        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return true;
    }
}

