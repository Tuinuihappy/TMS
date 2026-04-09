using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;
using Tms.Planning.Domain.Enums;

namespace Tms.Planning.Domain.Entities;

/// <summary>
/// RoutePlan — Aggregate Root สำหรับ Route Planning Phase 2
/// แต่ละ Plan คือเส้นทางของรถ 1 คัน ซึ่งมีหลาย Stop
/// </summary>
public sealed class RoutePlan : AggregateRoot
{
    public string PlanNumber { get; private set; } = string.Empty;
    public RoutePlanStatus Status { get; private set; }
    public Guid? VehicleTypeId { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal TotalDistanceKm { get; private set; }
    public int EstimatedTotalDurationMin { get; private set; }
    public decimal CapacityUtilizationPercent { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<RouteStop> _stops = [];
    public IReadOnlyCollection<RouteStop> Stops => _stops.AsReadOnly();

    private RoutePlan() { }

    public static RoutePlan Create(
        string planNumber,
        DateOnly plannedDate,
        Guid tenantId,
        Guid? vehicleTypeId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planNumber);
        return new RoutePlan
        {
            PlanNumber = planNumber,
            PlannedDate = plannedDate,
            TenantId = tenantId,
            VehicleTypeId = vehicleTypeId,
            Status = RoutePlanStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddStop(RouteStop stop) => _stops.Add(stop);

    public void SetStops(IEnumerable<RouteStop> ordered)
    {
        if (Status != RoutePlanStatus.Draft)
            throw new DomainException("Can only reorder stops on Draft plans.", "PLAN_NOT_DRAFT");
        _stops.Clear();
        _stops.AddRange(ordered);
    }

    public void UpdateMetrics(decimal distanceKm, int durationMin, decimal capacityPct)
    {
        TotalDistanceKm = distanceKm;
        EstimatedTotalDurationMin = durationMin;
        CapacityUtilizationPercent = capacityPct;
    }

    public void Lock()
    {
        if (Status != RoutePlanStatus.Draft)
            throw new DomainException(
                $"Cannot lock plan with status '{Status}'.",
                "PLAN_NOT_DRAFT");
        if (_stops.Count < 2)
            throw new DomainException("Plan must have at least 2 stops.", "INSUFFICIENT_STOPS");
        Status = RoutePlanStatus.Locked;
    }

    public void Discard()
    {
        if (Status == RoutePlanStatus.Locked)
            throw new DomainException("Cannot discard a locked plan.", "PLAN_LOCKED");
        Status = RoutePlanStatus.Discarded;
    }
}

/// <summary>One stop on a RoutePlan — can be Pickup or Dropoff</summary>
public sealed class RouteStop : BaseEntity
{
    public Guid RoutePlanId { get; private set; }
    public int Sequence { get; private set; }
    public Guid OrderId { get; private set; }
    /// <summary>"Pickup" | "Dropoff"</summary>
    public string StopType { get; private set; } = "Dropoff";
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public DateTime? EstimatedArrivalTime { get; private set; }
    public DateTime? EstimatedDepartureTime { get; private set; }

    private RouteStop() { }

    public static RouteStop Create(
        Guid routePlanId, int sequence, Guid orderId,
        string stopType,
        double lat, double lng,
        DateTime? etaArrival = null, DateTime? etaDeparture = null)
    {
        return new RouteStop
        {
            RoutePlanId = routePlanId,
            Sequence = sequence,
            OrderId = orderId,
            StopType = stopType,
            Latitude = lat,
            Longitude = lng,
            EstimatedArrivalTime = etaArrival,
            EstimatedDepartureTime = etaDeparture
        };
    }

    public void UpdateSequence(int seq) => Sequence = seq;
    public void UpdateEta(DateTime? arrival, DateTime? departure)
    {
        EstimatedArrivalTime = arrival;
        EstimatedDepartureTime = departure;
    }
}

/// <summary>Async optimization job record — tracks VRP job status</summary>
public sealed class OptimizationRequest : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public OptimizationStatus Status { get; private set; }
    public string? ParametersJson { get; private set; }   // input JSONB
    public string? ResultDataJson { get; private set; }   // output JSONB
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Resulting plans (after optimization completes)
    private readonly List<RoutePlan> _plans = [];
    public IReadOnlyCollection<RoutePlan> Plans => _plans.AsReadOnly();

    private OptimizationRequest() { }

    public static OptimizationRequest Create(Guid tenantId, string parametersJson)
    {
        return new OptimizationRequest
        {
            TenantId = tenantId,
            ParametersJson = parametersJson,
            Status = OptimizationStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing() => Status = OptimizationStatus.Processing;

    public void Complete(string resultJson, IEnumerable<RoutePlan> plans)
    {
        Status = OptimizationStatus.Completed;
        ResultDataJson = resultJson;
        CompletedAt = DateTime.UtcNow;
        _plans.AddRange(plans);
    }

    public void Fail(string errorMessage)
    {
        Status = OptimizationStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
