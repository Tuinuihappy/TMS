using Microsoft.EntityFrameworkCore;
using Tms.Planning.Application.Common.Interfaces;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.Application;

namespace Tms.Planning.Application.Features.Trips;

public sealed record GetTripsQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null,
    DateOnly? PlannedDate = null,
    Guid? TenantId = null
) : IQuery<PagedResult<TripDto>>;

public sealed class GetTripsHandler(IPlanningDbContext db)
    : IQueryHandler<GetTripsQuery, PagedResult<TripDto>>
{
    public async Task<PagedResult<TripDto>> Handle(GetTripsQuery request, CancellationToken ct)
    {
        var query = db.Trips.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TripStatus>(request.Status, ignoreCase: true, out var status))
            query = query.Where(t => t.Status == status);

        if (request.PlannedDate.HasValue)
            query = query.Where(t => DateOnly.FromDateTime(t.PlannedDate) == request.PlannedDate.Value);

        if (request.TenantId.HasValue)
            query = query.Where(t => t.TenantId == request.TenantId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ProjectToDto)
            .ToListAsync(ct);

        return PagedResult<TripDto>.Create(items, total, request.Page, request.PageSize);
    }

    internal static readonly System.Linq.Expressions.Expression<Func<Trip, TripDto>> ProjectToDto =
        t => new TripDto(
            t.Id, t.TripNumber, t.Status.ToString(),
            t.VehicleId, t.DriverId, t.PlannedDate,
            t.TotalWeight, t.TotalVolumeCBM,
            t.TotalDistanceKm, t.EstimatedDurationMin,
            t.CancelReason, t.DispatchedAt, t.CompletedAt, t.CreatedAt,
            t.Stops.OrderBy(s => s.Sequence)
                .Select(s => new StopDto(
                    s.Id, s.Sequence, s.OrderId,
                    s.Type.ToString(), s.Status.ToString(),
                    s.AddressName, s.AddressProvince,
                    s.WindowFrom, s.WindowTo, s.ArrivalAt, s.DepartureAt))
                .ToList());
}

public sealed record GetTripByIdQuery(Guid TripId) : IQuery<TripDto?>;

public sealed class GetTripByIdHandler(IPlanningDbContext db)
    : IQueryHandler<GetTripByIdQuery, TripDto?>
{
    public async Task<TripDto?> Handle(GetTripByIdQuery request, CancellationToken ct) =>
        await db.Trips
            .AsNoTracking()
            .Where(t => t.Id == request.TripId)
            .Select(GetTripsHandler.ProjectToDto)
            .FirstOrDefaultAsync(ct);
}

public sealed record GetDispatchBoardQuery(DateOnly Date, Guid TenantId)
    : IQuery<DispatchBoardDto>;

public sealed class GetDispatchBoardHandler(IPlanningDbContext db)
    : IQueryHandler<GetDispatchBoardQuery, DispatchBoardDto>
{
    public async Task<DispatchBoardDto> Handle(GetDispatchBoardQuery request, CancellationToken ct)
    {
        var dtos = await db.Trips
            .AsNoTracking()
            .Where(t => DateOnly.FromDateTime(t.PlannedDate) == request.Date
                     && t.TenantId == request.TenantId)
            .OrderBy(t => t.TripNumber)
            .Select(GetTripsHandler.ProjectToDto)
            .ToListAsync(ct);

        var summary = new DispatchBoardSummary(
            dtos.Count,
            dtos.Count(t => t.Status == "Dispatched"),
            dtos.Count(t => t.Status is "Created" or "Assigned"));

        return new DispatchBoardDto(request.Date.ToString("yyyy-MM-dd"), dtos, summary);
    }
}
