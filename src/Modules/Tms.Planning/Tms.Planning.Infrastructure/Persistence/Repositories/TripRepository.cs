using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.Planning.Infrastructure.Persistence;

namespace Tms.Planning.Infrastructure.Persistence.Repositories;

public sealed class TripRepository(PlanningDbContext context) : ITripRepository
{
    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Trips.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Trip?> GetByTripNumberAsync(string tripNumber, CancellationToken ct = default) =>
        await context.Trips.FirstOrDefaultAsync(t => t.TripNumber == tripNumber, ct);

    public async Task<(IReadOnlyList<Trip> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        DateOnly? plannedDate = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = context.Trips.Include(t => t.Stops).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status.ToString() == status);
        if (plannedDate.HasValue)
            query = query.Where(t => DateOnly.FromDateTime(t.PlannedDate) == plannedDate.Value);
        if (tenantId.HasValue)
            query = query.Where(t => t.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Trip>> GetByDateAsync(DateOnly date, Guid tenantId, CancellationToken ct = default) =>
        await context.Trips
            .Include(t => t.Stops)
            .Where(t => DateOnly.FromDateTime(t.PlannedDate) == date && t.TenantId == tenantId)
            .OrderBy(t => t.TripNumber)
            .ToListAsync(ct);

    public async Task AddAsync(Trip entity, CancellationToken ct = default)
    {
        await context.Trips.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Trip entity, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Trip entity, CancellationToken ct = default)
    {
        context.Trips.Remove(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateTripNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"TRP-{today:yyyyMMdd}";
        var count = await context.Trips
            .CountAsync(t => t.TripNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D3}";
    }

    public async Task AddStopAsync(Stop stop, CancellationToken ct = default)
    {
        await context.Stops.AddAsync(stop, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Trip?> GetByStopIdAsync(Guid stopId, CancellationToken ct = default) =>
        await context.Trips
            .Include(t => t.Stops)
            .FirstOrDefaultAsync(t => t.Stops.Any(s => s.Id == stopId), ct);
}
