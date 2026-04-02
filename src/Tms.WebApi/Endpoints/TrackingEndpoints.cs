using MediatR;
using Tms.Tracking.Application.Features;

namespace Tms.WebApi.Endpoints;

public static class TrackingEndpoints
{
    public static IEndpointRouteBuilder MapTrackingEndpoints(this IEndpointRouteBuilder app)
    {
        var zones = app.MapGroup("/api/tracking/zones").WithTags("Tracking");
        var vehicles = app.MapGroup("/api/tracking/vehicles").WithTags("Tracking");
        var positions = app.MapGroup("/api/tracking/positions").WithTags("Tracking");
        var orders = app.MapGroup("/api/tracking/orders").WithTags("Tracking");

        // POST /api/tracking/positions  — Batch GPS ingest
        positions.MapPost("/", async (
            IngestPositionRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new IngestPositionsCommand(
                req.VehicleId, req.TenantId, req.TripId,
                req.Positions.Select(p =>
                    new PositionInputDto(p.Lat, p.Lng, p.Speed, p.Heading, p.IsEngineOn, p.Timestamp))
                    .ToList()), ct);
            return Results.NoContent();
        })
        .WithName("IngestPositions")
        .WithSummary("ส่ง GPS Batch (Driver App / IoT)");

        // GET /api/tracking/vehicles  — Live Map
        vehicles.MapGet("/", async (
            ISender sender, Guid? tenantId, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetLiveMapQuery(tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetLiveVehicleMap")
        .WithSummary("ตำแหน่งรถล่าสุดทั้งหมด (Live Map)");

        // GET /api/tracking/vehicles/{id}/history  — Route Playback
        vehicles.MapGet("/{id:guid}/history", async (
            Guid id, ISender sender,
            DateTime? from, DateTime? to,
            CancellationToken ct) =>
        {
            var fromDt = from ?? DateTime.UtcNow.AddHours(-8);
            var toDt = to ?? DateTime.UtcNow;
            var result = await sender.Send(new GetVehicleHistoryQuery(id, fromDt, toDt), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetVehicleHistory")
        .WithSummary("ย้อนดูเส้นทางรถ (Route Playback)");

        // GET /api/tracking/orders/{orderId}/eta
        orders.MapGet("/{orderId:guid}/eta", async (
            Guid orderId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrderEtaQuery(orderId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetOrderEta")
        .WithSummary("คำนวณ ETA สินค้า");

        // POST /api/tracking/zones  — Create GeoZone
        zones.MapPost("/", async (
            CreateGeoZoneRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateGeoZoneCommand(
                req.Name, req.TenantId, req.Type, req.LocationId,
                req.CenterLat, req.CenterLng, req.RadiusMeters, req.PolygonCoordinatesJson), ct);
            return Results.Created($"/api/tracking/zones/{id}", new { Id = id });
        })
        .WithName("CreateGeoZone")
        .WithSummary("สร้าง GeoZone");

        // GET /api/tracking/zones
        zones.MapGet("/", async (
            ISender sender, Guid? tenantId, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGeoZonesQuery(tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetGeoZones")
        .WithSummary("รายการ GeoZone ทั้งหมด");

        // PUT /api/tracking/zones/{id}
        zones.MapPut("/{id:guid}", async (
            Guid id, UpdateGeoZoneRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateGeoZoneCommand(
                id, req.Name, req.CenterLat, req.CenterLng,
                req.RadiusMeters, req.PolygonCoordinatesJson), ct);
            return Results.NoContent();
        })
        .WithName("UpdateGeoZone")
        .WithSummary("แก้ไขขอบเขต GeoZone");

        return app;
    }
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record PositionPointRequest(
    double Lat, double Lng, decimal Speed, decimal Heading,
    bool IsEngineOn, DateTime Timestamp);

public sealed record IngestPositionRequest(
    Guid VehicleId, Guid TenantId, Guid? TripId,
    List<PositionPointRequest> Positions);

public sealed record CreateGeoZoneRequest(
    string Name, Guid TenantId, string Type,
    Guid? LocationId,
    double? CenterLat, double? CenterLng, double? RadiusMeters,
    string? PolygonCoordinatesJson);

public sealed record UpdateGeoZoneRequest(
    string Name,
    double? CenterLat, double? CenterLng, double? RadiusMeters,
    string? PolygonCoordinatesJson);
