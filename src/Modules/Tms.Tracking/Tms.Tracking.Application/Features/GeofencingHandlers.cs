using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;
using Tms.Tracking.Domain.Entities;
using Tms.Tracking.Domain.Enums;
using Tms.Tracking.Domain.Interfaces;

namespace Tms.Tracking.Application.Features;

// ── Shared DTOs ──────────────────────────────────────────────────────────────

public sealed record GeoZoneDto(
    Guid Id, string Name, string Type,
    Guid? LocationId, bool IsActive,
    double? CenterLat, double? CenterLng, double? RadiusMeters,
    string? PolygonCoordinatesJson);

// ── Commands / Queries ───────────────────────────────────────────────────────

// POST /api/tracking/zones
public sealed record CreateGeoZoneCommand(
    string Name,
    Guid TenantId,
    string Type,                    // "Circle" | "Polygon"
    Guid? LocationId,
    double? CenterLat,
    double? CenterLng,
    double? RadiusMeters,
    string? PolygonCoordinatesJson) : ICommand<Guid>;

public sealed class CreateGeoZoneHandler(IGeoZoneRepository repo)
    : ICommandHandler<CreateGeoZoneCommand, Guid>
{
    public async Task<Guid> Handle(CreateGeoZoneCommand req, CancellationToken ct)
    {
        var zoneType = Enum.Parse<GeoZoneType>(req.Type, ignoreCase: true);

        GeoZone zone = zoneType == GeoZoneType.Circle
            ? GeoZone.CreateCircle(
                req.Name, req.TenantId,
                req.CenterLat ?? throw new ArgumentNullException(nameof(req.CenterLat)),
                req.CenterLng ?? throw new ArgumentNullException(nameof(req.CenterLng)),
                req.RadiusMeters ?? throw new ArgumentNullException(nameof(req.RadiusMeters)),
                req.LocationId)
            : GeoZone.CreatePolygon(
                req.Name, req.TenantId,
                req.PolygonCoordinatesJson ?? throw new ArgumentNullException(nameof(req.PolygonCoordinatesJson)),
                req.LocationId);

        await repo.AddAsync(zone, ct);
        return zone.Id;
    }
}

// PUT /api/tracking/zones/{id}
public sealed record UpdateGeoZoneCommand(
    Guid Id, string Name,
    double? CenterLat, double? CenterLng, double? RadiusMeters,
    string? PolygonCoordinatesJson) : ICommand;

public sealed class UpdateGeoZoneHandler(IGeoZoneRepository repo)
    : ICommandHandler<UpdateGeoZoneCommand>
{
    public async Task Handle(UpdateGeoZoneCommand req, CancellationToken ct)
    {
        var zone = await repo.GetByIdAsync(req.Id, ct)
            ?? throw new SharedKernel.Exceptions.NotFoundException(nameof(GeoZone), req.Id);
        zone.Update(req.Name, req.CenterLat, req.CenterLng, req.RadiusMeters, req.PolygonCoordinatesJson);
        await repo.UpdateAsync(zone, ct);
    }
}

// GET /api/tracking/zones
public sealed record GetGeoZonesQuery(Guid? TenantId) : IQuery<List<GeoZoneDto>>;

public sealed class GetGeoZonesHandler(IGeoZoneRepository repo)
    : IQueryHandler<GetGeoZonesQuery, List<GeoZoneDto>>
{
    public async Task<List<GeoZoneDto>> Handle(GetGeoZonesQuery req, CancellationToken ct)
    {
        var zones = await repo.GetActiveAsync(req.TenantId, ct);
        return zones.Select(z => new GeoZoneDto(
            z.Id, z.Name, z.Type.ToString(), z.LocationId, z.IsActive,
            z.CenterLatitude, z.CenterLongitude, z.RadiusMeters,
            z.PolygonCoordinatesJson))
            .ToList();
    }
}

/// <summary>
/// Geofence check command — called by GeofenceBackgroundWorker.
/// Takes the latest vehicle positions and checks each against all active GeoZones.
/// Fires VehicleEnteredZoneIntegrationEvent for new Enter events.
/// </summary>
public sealed record RunGeofenceChecksCommand(Guid TenantId) : ICommand;

public sealed class RunGeofenceChecksHandler(
    IGeoZoneRepository zoneRepo,
    ICurrentVehicleStateRepository stateRepo,
    IZoneEventRepository zoneEventRepo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<RunGeofenceChecksCommand>
{
    public async Task Handle(RunGeofenceChecksCommand req, CancellationToken ct)
    {
        var zones = await zoneRepo.GetActiveAsync(req.TenantId, ct);
        if (zones.Count == 0) return;

        var vehicles = await stateRepo.GetAllAsync(req.TenantId, ct);
        if (vehicles.Count == 0) return;

        foreach (var zone in zones)
        {
            foreach (var vehicle in vehicles)
            {
                bool isInside = zone.CheckPointInside(vehicle.Latitude, vehicle.Longitude);
                if (!isInside) continue;

                // Check if we already recorded an Enter recently (dedup)
                var latestEvent = await zoneEventRepo.GetLatestAsync(zone.Id, vehicle.VehicleId, ct);
                if (latestEvent is not null && latestEvent.EventType == ZoneEventType.Enter)
                    continue; // Already inside — skip duplicate

                // Record new Enter event
                var zoneEvent = ZoneEvent.Create(
                    zone.Id, vehicle.VehicleId, ZoneEventType.Enter, req.TenantId);
                await zoneEventRepo.AddAsync(zoneEvent, ct);

                // Publish integration event so Execution can auto-arrive shipments
                if (zone.LocationId.HasValue)
                {
                    await eventPublisher.PublishAsync(
                        new VehicleEnteredZoneIntegrationEvent(
                            vehicle.VehicleId,
                            zone.Id,
                            zone.LocationId.Value,
                            DateTime.UtcNow,
                            req.TenantId), ct);
                }
            }
        }
    }
}
