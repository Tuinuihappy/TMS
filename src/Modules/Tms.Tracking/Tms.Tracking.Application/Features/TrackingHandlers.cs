using Tms.SharedKernel.Application;
using Tms.Tracking.Domain.Entities;
using Tms.Tracking.Domain.Interfaces;

namespace Tms.Tracking.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record PositionInputDto(
    double Lat, double Lng,
    decimal Speed, decimal Heading,
    bool IsEngineOn,
    DateTime Timestamp);

public sealed record VehicleStateDto(
    Guid VehicleId, double Lat, double Lng,
    decimal SpeedKmh, DateTime LastUpdated);

public sealed record PositionHistoryDto(
    Guid Id, Guid VehicleId,
    double Lat, double Lng,
    decimal SpeedKmh, decimal Heading,
    bool IsEngineOn, DateTime Timestamp);

// ── Commands / Queries ───────────────────────────────────────────────────────

// POST /api/tracking/positions  (Batch GPS ingest)
public sealed record IngestPositionsCommand(
    Guid VehicleId,
    Guid TenantId,
    Guid? TripId,
    List<PositionInputDto> Positions) : ICommand;

public sealed class IngestPositionsHandler(
    IVehiclePositionRepository positionRepo,
    ICurrentVehicleStateRepository stateRepo)
    : ICommandHandler<IngestPositionsCommand>
{
    public async Task Handle(IngestPositionsCommand req, CancellationToken ct)
    {
        if (req.Positions.Count == 0) return;

        // Sort incoming batch by time ascending
        var sorted = req.Positions.OrderBy(p => p.Timestamp).ToList();

        var entities = sorted.Select(p =>
            VehiclePosition.Create(
                req.VehicleId, p.Lat, p.Lng,
                p.Speed, p.Heading, p.IsEngineOn, p.Timestamp, req.TripId))
            .ToList();

        await positionRepo.BulkInsertAsync(entities, ct);

        // UPSERT latest state from newest point in batch
        var latest = sorted[^1];
        var existing = await stateRepo.GetByVehicleIdAsync(req.VehicleId, ct);

        if (existing is null)
        {
            var state = CurrentVehicleState.Create(
                req.VehicleId, req.TenantId,
                latest.Lat, latest.Lng, latest.Speed, latest.Timestamp);
            await stateRepo.UpsertAsync(state, ct);
        }
        else if (latest.Timestamp > existing.LastUpdatedAt)
        {
            existing.Update(latest.Lat, latest.Lng, latest.Speed, latest.Timestamp);
            await stateRepo.UpsertAsync(existing, ct);
        }
    }
}

// GET /api/tracking/vehicles  (Live Map)
public sealed record GetLiveMapQuery(Guid? TenantId) : IQuery<List<VehicleStateDto>>;

public sealed class GetLiveMapHandler(ICurrentVehicleStateRepository repo)
    : IQueryHandler<GetLiveMapQuery, List<VehicleStateDto>>
{
    public async Task<List<VehicleStateDto>> Handle(GetLiveMapQuery req, CancellationToken ct)
    {
        var states = await repo.GetAllAsync(req.TenantId, ct);
        return states.Select(s => new VehicleStateDto(
            s.VehicleId, s.Latitude, s.Longitude, s.SpeedKmh, s.LastUpdatedAt))
            .ToList();
    }
}

// GET /api/tracking/vehicles/{id}/history  (Route Playback)
public sealed record GetVehicleHistoryQuery(
    Guid VehicleId, DateTime From, DateTime To)
    : IQuery<List<PositionHistoryDto>>;

public sealed class GetVehicleHistoryHandler(IVehiclePositionRepository repo)
    : IQueryHandler<GetVehicleHistoryQuery, List<PositionHistoryDto>>
{
    public async Task<List<PositionHistoryDto>> Handle(GetVehicleHistoryQuery req, CancellationToken ct)
    {
        var items = await repo.GetHistoryAsync(req.VehicleId, req.From, req.To, ct);
        return items.Select(p => new PositionHistoryDto(
            p.Id, p.VehicleId, p.Latitude, p.Longitude,
            p.SpeedKmh, p.Heading, p.IsEngineOn, p.Timestamp))
            .ToList();
    }
}

// GET /api/tracking/orders/{orderId}/eta  (ETA stub)
public sealed record GetOrderEtaQuery(Guid OrderId) : IQuery<EtaDto?>;

public sealed record EtaDto(Guid OrderId, DateTime? EstimatedArrivalTime, string Note);

public sealed class GetOrderEtaHandler : IQueryHandler<GetOrderEtaQuery, EtaDto?>
{
    public Task<EtaDto?> Handle(GetOrderEtaQuery req, CancellationToken ct)
    {
        // Stub: real implementation calls Maps API (e.g. OSRM / Google Maps)
        var eta = new EtaDto(
            req.OrderId,
            DateTime.UtcNow.AddHours(2),
            "ETA estimated from latest vehicle position (stub implementation)");
        return Task.FromResult<EtaDto?>(eta);
    }
}
