using Tms.Tracking.Domain.Entities;

namespace Tms.Tracking.Domain.Interfaces;

public interface IVehiclePositionRepository
{
    Task BulkInsertAsync(IEnumerable<VehiclePosition> positions, CancellationToken ct = default);
    Task<IReadOnlyList<VehiclePosition>> GetHistoryAsync(Guid vehicleId, DateTime from, DateTime to, CancellationToken ct = default);
}

public interface ICurrentVehicleStateRepository
{
    Task<IReadOnlyList<CurrentVehicleState>> GetAllAsync(Guid? tenantId, CancellationToken ct = default);
    Task<CurrentVehicleState?> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);
    Task UpsertAsync(CurrentVehicleState state, CancellationToken ct = default);
}

public interface IGeoZoneRepository
{
    Task<GeoZone?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<GeoZone>> GetActiveAsync(Guid? tenantId, CancellationToken ct = default);
    Task AddAsync(GeoZone zone, CancellationToken ct = default);
    Task UpdateAsync(GeoZone zone, CancellationToken ct = default);
}

public interface IZoneEventRepository
{
    Task AddAsync(ZoneEvent zoneEvent, CancellationToken ct = default);
    Task<ZoneEvent?> GetLatestAsync(Guid zoneId, Guid vehicleId, CancellationToken ct = default);
}
