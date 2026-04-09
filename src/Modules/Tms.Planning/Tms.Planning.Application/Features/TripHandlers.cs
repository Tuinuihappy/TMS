using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────
public sealed record StopDto(
    Guid Id, int Sequence, Guid OrderId, string Type, string Status,
    string? AddressName, string? AddressProvince,
    DateTime? WindowFrom, DateTime? WindowTo,
    DateTime? ArrivalAt, DateTime? DepartureAt);

public sealed record TripDto(
    Guid Id, string TripNumber, string Status,
    Guid? VehicleId, Guid? DriverId,
    DateTime PlannedDate,
    decimal TotalWeight, decimal TotalVolumeCBM,
    decimal? TotalDistanceKm, int? EstimatedDurationMin,
    string? CancelReason,
    DateTime? DispatchedAt, DateTime? CompletedAt,
    DateTime CreatedAt,
    List<StopDto> Stops);

// ── AddStop Request ───────────────────────────────────────────────────────
public sealed record AddStopInput(
    int Sequence, Guid OrderId, string Type,
    string? AddressName, string? AddressStreet, string? AddressProvince,
    double? Lat, double? Lng,
    DateTime? WindowFrom, DateTime? WindowTo);

// ─────────────────────────────────────────────────────────────────────────
// COMMANDS
// ─────────────────────────────────────────────────────────────────────────

// ── Create Trip ───────────────────────────────────────────────────────────
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

// ── Add Stop ──────────────────────────────────────────────────────────────
public sealed record AddStopCommand(Guid TripId, AddStopInput Stop) : ICommand<Guid>;

public sealed class AddStopHandler(ITripRepository repo)
    : ICommandHandler<AddStopCommand, Guid>
{
    public async Task<Guid> Handle(AddStopCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        // Validate trip state
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
    Guid TripId, Guid VehicleId, Guid DriverId) : ICommand;

public sealed class AssignResourcesHandler(
    ITripRepository repo,
    IResourceAvailabilityChecker? resourceChecker = null)
    : ICommandHandler<AssignResourcesCommand>
{
    public async Task Handle(AssignResourcesCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        // Cross-module validation (if checker is registered)
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

// ── Dispatch Trip ─────────────────────────────────────────────────────────
public sealed record DispatchTripCommand(Guid TripId) : ICommand;

public sealed class DispatchTripHandler(
    ITripRepository repo,
    IOutboxWriter outbox)
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

        // Stage event BEFORE SaveChanges → written atomically with the Trip update
        outbox.Stage(new TripDispatchedIntegrationEvent(
            trip.Id, trip.TripNumber,
            trip.VehicleId!.Value, trip.DriverId!.Value,
            trip.TenantId, stops));

        await repo.UpdateAsync(trip, ct);
    }
}

// ── Complete Trip ─────────────────────────────────────────────────────────
public sealed record CompleteTripCommand(Guid TripId) : ICommand;

public sealed class CompleteTripHandler(
    ITripRepository repo,
    IOutboxWriter outbox)
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

// ── Cancel Trip ───────────────────────────────────────────────────────────
public sealed record CancelTripCommand(Guid TripId, string Reason) : ICommand;

public sealed class CancelTripHandler(
    ITripRepository repo,
    IOutboxWriter outbox)
    : ICommandHandler<CancelTripCommand>
{
    public async Task Handle(CancelTripCommand request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct)
            ?? throw new NotFoundException(nameof(Trip), request.TripId);

        trip.Cancel(request.Reason);
        outbox.Stage(new TripCancelledIntegrationEvent(
            trip.Id, trip.TripNumber, request.Reason,
            trip.VehicleId, trip.DriverId));

        await repo.UpdateAsync(trip, ct);
    }
}


// ─────────────────────────────────────────────────────────────────────────
// QUERIES
// ─────────────────────────────────────────────────────────────────────────

// ── Get Trips (Paged) ─────────────────────────────────────────────────────
public sealed record GetTripsQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null,
    DateOnly? PlannedDate = null,
    Guid? TenantId = null
) : IQuery<PagedResult<TripDto>>;

public sealed class GetTripsHandler(ITripRepository repo)
    : IQueryHandler<GetTripsQuery, PagedResult<TripDto>>
{
    public async Task<PagedResult<TripDto>> Handle(GetTripsQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.PlannedDate, request.TenantId, ct);

        return PagedResult<TripDto>.Create(
            items.Select(MapDto).ToList(),
            total, request.Page, request.PageSize);
    }

    internal static TripDto MapDto(Trip t) => new(
        t.Id, t.TripNumber, t.Status.ToString(),
        t.VehicleId, t.DriverId, t.PlannedDate,
        t.TotalWeight, t.TotalVolumeCBM,
        t.TotalDistanceKm, t.EstimatedDurationMin,
        t.CancelReason, t.DispatchedAt, t.CompletedAt, t.CreatedAt,
        t.Stops.OrderBy(s => s.Sequence).Select(s => new StopDto(
            s.Id, s.Sequence, s.OrderId, s.Type.ToString(), s.Status.ToString(),
            s.AddressName, s.AddressProvince,
            s.WindowFrom, s.WindowTo, s.ArrivalAt, s.DepartureAt)).ToList());
}

// ── Get Trip By Id ────────────────────────────────────────────────────────
public sealed record GetTripByIdQuery(Guid TripId) : IQuery<TripDto?>;

public sealed class GetTripByIdHandler(ITripRepository repo)
    : IQueryHandler<GetTripByIdQuery, TripDto?>
{
    public async Task<TripDto?> Handle(GetTripByIdQuery request, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(request.TripId, ct);
        return trip is null ? null : GetTripsHandler.MapDto(trip);
    }
}

// ── Get Dispatch Board ────────────────────────────────────────────────────
public sealed record DispatchBoardDto(
    string Date,
    List<TripDto> Trips,
    DispatchBoardSummary Summary);

public sealed record DispatchBoardSummary(int Total, int Dispatched, int Pending);

public sealed record GetDispatchBoardQuery(DateOnly Date, Guid TenantId)
    : IQuery<DispatchBoardDto>;

public sealed class GetDispatchBoardHandler(ITripRepository repo)
    : IQueryHandler<GetDispatchBoardQuery, DispatchBoardDto>
{
    public async Task<DispatchBoardDto> Handle(GetDispatchBoardQuery request, CancellationToken ct)
    {
        var trips = await repo.GetByDateAsync(request.Date, request.TenantId, ct);
        var dtos = trips.Select(GetTripsHandler.MapDto).ToList();
        var summary = new DispatchBoardSummary(
            dtos.Count,
            dtos.Count(t => t.Status == "Dispatched"),
            dtos.Count(t => t.Status is "Created" or "Assigned"));

        return new DispatchBoardDto(request.Date.ToString("yyyy-MM-dd"), dtos, summary);
    }
}
