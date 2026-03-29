using Tms.Planning.Domain.Enums;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Planning.Domain.Entities;

public sealed class Trip : AggregateRoot
{
    public string TripNumber { get; private set; } = string.Empty;
    public TripStatus Status { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? DriverId { get; private set; }
    public DateTime PlannedDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Guid> _orderIds = [];
    public IReadOnlyCollection<Guid> OrderIds => _orderIds.AsReadOnly();

    private Trip() { } // EF Core

    public static Trip Create(string tripNumber, DateTime plannedDate, IEnumerable<Guid> orderIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripNumber);

        var trip = new Trip
        {
            TripNumber = tripNumber,
            Status = TripStatus.Created,
            PlannedDate = plannedDate,
            CreatedAt = DateTime.UtcNow
        };
        trip._orderIds.AddRange(orderIds);
        return trip;
    }

    public void AssignResources(Guid vehicleId, Guid driverId)
    {
        if (Status != TripStatus.Created)
            throw new DomainException("Resources can only be assigned to Created trips.", "TRIP_CANNOT_ASSIGN");

        VehicleId = vehicleId;
        DriverId = driverId;
        Status = TripStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Dispatch()
    {
        if (Status != TripStatus.Assigned)
            throw new DomainException("Only Assigned trips can be dispatched.", "TRIP_CANNOT_DISPATCH");

        Status = TripStatus.Dispatched;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is TripStatus.Completed or TripStatus.Cancelled)
            throw new DomainException($"Cannot cancel trip with status '{Status}'.", "TRIP_CANNOT_CANCEL");

        Status = TripStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
