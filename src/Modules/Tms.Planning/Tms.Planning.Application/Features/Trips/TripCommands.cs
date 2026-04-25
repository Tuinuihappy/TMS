using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features.Trips;

public sealed record CreateTripCommand(
    DateTime PlannedDate,
    Guid TenantId,
    decimal TotalWeight = 0,
    decimal TotalVolumeCBM = 0,
    Guid? CreatedBy = null,
    List<AddStopInput>? InitialStops = null
) : ICommand<Guid>;

public sealed class CreateTripHandler(ITripRepository repo)
    : ICommandHandler<CreateTripCommand, Guid>
{
    public async Task<Guid> Handle(CreateTripCommand request, CancellationToken ct)
    {
        var tripNumber = await repo.GenerateTripNumberAsync(ct);
        var trip = Trip.Create(
            tripNumber, request.PlannedDate, request.TenantId,
            request.TotalWeight, request.TotalVolumeCBM, request.CreatedBy);

        foreach (var s in request.InitialStops ?? [])
        {
            var stopType = Enum.Parse<StopType>(s.Type, ignoreCase: true);
            trip.AddStop(s.Sequence, s.OrderId, stopType,
                s.AddressName, s.AddressStreet, s.AddressProvince,
                s.Lat, s.Lng, s.WindowFrom, s.WindowTo);
        }

        await repo.AddAsync(trip, ct);
        return trip.Id;
    }
}

public sealed record AddStopCommand(Guid TripId, AddStopInput Stop) : ICommand<Guid>;

public sealed class AddStopHandler(ITripRepository repo)
    : ICommandHandler<AddStopCommand, Guid>
{
    public async Task<Guid> Handle(AddStopCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        if (trip.Status is not (TripStatus.Created or TripStatus.Assigned))
            throw new DomainException("Cannot add stops to trip in current status.", "INVALID_TRIP_STATE");

        var stopType = Enum.Parse<StopType>(request.Stop.Type, ignoreCase: true);
        var stop = Stop.Create(
            request.TripId, request.Stop.Sequence, request.Stop.OrderId, stopType,
            request.Stop.AddressName, request.Stop.AddressStreet, request.Stop.AddressProvince,
            request.Stop.Lat, request.Stop.Lng, request.Stop.WindowFrom, request.Stop.WindowTo);

        await repo.AddStopAsync(stop, ct);
        return stop.Id;
    }
}

public sealed record AssignResourcesCommand(
    Guid TripId, Guid VehicleId, Guid DriverId) : ICommand, IAuditableCommand
{
    public string ResourceName => "Trip";
    public string? ResourceId => TripId.ToString();
}

public sealed class AssignResourcesHandler(
    ITripRepository repo,
    IResourceAvailabilityChecker? resourceChecker = null)
    : ICommandHandler<AssignResourcesCommand>
{
    public async Task Handle(AssignResourcesCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        if (resourceChecker is not null)
        {
            if (!await resourceChecker.IsVehicleAvailableAsync(request.VehicleId, ct))
                throw new DomainException("Vehicle is not available for assignment.", "VEHICLE_NOT_AVAILABLE");
            if (!await resourceChecker.IsDriverAvailableAsync(request.DriverId, ct))
                throw new DomainException("Driver is not available for assignment.", "DRIVER_NOT_AVAILABLE");
        }

        trip.AssignResources(request.VehicleId, request.DriverId);
        await repo.UpdateAsync(trip, ct);
    }
}

public sealed record DispatchTripCommand(Guid TripId) : ICommand, IAuditableCommand
{
    public string ResourceName => "Trip";
    public string? ResourceId => TripId.ToString();
}

public sealed class DispatchTripHandler(ITripRepository repo, IOutboxWriter outbox)
    : ICommandHandler<DispatchTripCommand>
{
    public async Task Handle(DispatchTripCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        trip.Dispatch();

        var stops = trip.Stops.Select(s => new TripStopSnapshot(
            s.Id, s.Sequence, s.OrderId, s.Type.ToString(),
            s.AddressName, s.AddressStreet, s.AddressProvince,
            s.AddressLatitude, s.AddressLongitude)).ToList();

        outbox.Stage(new TripDispatchedIntegrationEvent(
            trip.Id, trip.TripNumber,
            trip.VehicleId!.Value, trip.DriverId!.Value,
            trip.TenantId, stops));

        await repo.UpdateAsync(trip, ct);
    }
}

public sealed record CompleteTripCommand(Guid TripId) : ICommand;

public sealed class CompleteTripHandler(ITripRepository repo, IOutboxWriter outbox)
    : ICommandHandler<CompleteTripCommand>
{
    public async Task Handle(CompleteTripCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        trip.Complete();
        outbox.Stage(new TripCompletedIntegrationEvent(
            trip.Id, trip.TripNumber, trip.VehicleId, trip.DriverId));

        await repo.UpdateAsync(trip, ct);
    }
}

public sealed record CancelTripCommand(Guid TripId, string Reason) : ICommand, IAuditableCommand
{
    public string ResourceName => "Trip";
    public string? ResourceId => TripId.ToString();
}

public sealed class CancelTripHandler(ITripRepository repo, IOutboxWriter outbox)
    : ICommandHandler<CancelTripCommand>
{
    public async Task Handle(CancelTripCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        var stops = trip.Stops.Select(s => new TripStopSnapshot(
            s.Id, s.Sequence, s.OrderId, s.Type.ToString(),
            s.AddressName, s.AddressStreet, s.AddressProvince,
            s.AddressLatitude, s.AddressLongitude)).ToList();

        trip.Cancel(request.Reason);
        outbox.Stage(new TripCancelledIntegrationEvent(
            trip.Id, trip.TripNumber, request.Reason, stops,
            trip.VehicleId, trip.DriverId));

        await repo.UpdateAsync(trip, ct);
    }
}
