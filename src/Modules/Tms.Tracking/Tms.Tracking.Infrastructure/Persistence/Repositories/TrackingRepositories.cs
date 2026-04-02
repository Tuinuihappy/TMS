using Microsoft.EntityFrameworkCore;
using Tms.Tracking.Domain.Entities;
using Tms.Tracking.Domain.Enums;
using Tms.Tracking.Domain.Interfaces;
using Tms.Tracking.Infrastructure.Persistence;

namespace Tms.Tracking.Infrastructure.Persistence.Repositories;

public sealed class VehiclePositionRepository(TrackingDbContext context) : IVehiclePositionRepository
{
    public async Task BulkInsertAsync(IEnumerable<VehiclePosition> positions, CancellationToken ct = default)
    {
        await context.VehiclePositions.AddRangeAsync(positions, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<VehiclePosition>> GetHistoryAsync(
        Guid vehicleId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await context.VehiclePositions
            .Where(p => p.VehicleId == vehicleId
                     && p.Timestamp >= from
                     && p.Timestamp <= to)
            .OrderBy(p => p.Timestamp)
            .ToListAsync(ct);
    }
}

public sealed class CurrentVehicleStateRepository(TrackingDbContext context) : ICurrentVehicleStateRepository
{
    public async Task<IReadOnlyList<CurrentVehicleState>> GetAllAsync(
        Guid? tenantId, CancellationToken ct = default)
    {
        var query = context.CurrentVehicleStates.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(s => s.TenantId == tenantId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task<CurrentVehicleState?> GetByVehicleIdAsync(
        Guid vehicleId, CancellationToken ct = default)
        => await context.CurrentVehicleStates.FindAsync([vehicleId], ct);

    public async Task UpsertAsync(CurrentVehicleState state, CancellationToken ct = default)
    {
        var exists = await context.CurrentVehicleStates
            .AnyAsync(s => s.VehicleId == state.VehicleId, ct);

        if (exists)
            context.CurrentVehicleStates.Update(state);
        else
            context.CurrentVehicleStates.Add(state);

        await context.SaveChangesAsync(ct);
    }
}

public sealed class GeoZoneRepository(TrackingDbContext context) : IGeoZoneRepository
{
    public async Task<GeoZone?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.GeoZones.FindAsync([id], ct);

    public async Task<IReadOnlyList<GeoZone>> GetActiveAsync(
        Guid? tenantId, CancellationToken ct = default)
    {
        var query = context.GeoZones.Where(z => z.IsActive);
        if (tenantId.HasValue)
            query = query.Where(z => z.TenantId == tenantId.Value);
        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(GeoZone zone, CancellationToken ct = default)
    {
        context.GeoZones.Add(zone);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(GeoZone zone, CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}

public sealed class ZoneEventRepository(TrackingDbContext context) : IZoneEventRepository
{
    public async Task AddAsync(ZoneEvent zoneEvent, CancellationToken ct = default)
    {
        context.ZoneEvents.Add(zoneEvent);
        await context.SaveChangesAsync(ct);
    }

    public async Task<ZoneEvent?> GetLatestAsync(
        Guid zoneId, Guid vehicleId, CancellationToken ct = default)
        => await context.ZoneEvents
            .Where(e => e.ZoneId == zoneId && e.VehicleId == vehicleId)
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(ct);
}
