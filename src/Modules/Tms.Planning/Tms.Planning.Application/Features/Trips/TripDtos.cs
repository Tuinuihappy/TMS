namespace Tms.Planning.Application.Features.Trips;

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

public sealed record AddStopInput(
    int Sequence, Guid OrderId, string Type,
    string? AddressName, string? AddressStreet, string? AddressProvince,
    double? Lat, double? Lng,
    DateTime? WindowFrom, DateTime? WindowTo);

public sealed record DispatchBoardDto(
    string Date,
    List<TripDto> Trips,
    DispatchBoardSummary Summary);

public sealed record DispatchBoardSummary(int Total, int Dispatched, int Pending);
