using MediatR;
using Tms.Planning.Application.Features;

namespace Tms.WebApi.Endpoints;

public record CreateTripRequest(
    DateTime PlannedDate,
    Guid TenantId,
    List<AddStopInput>? Stops = null,
    decimal TotalWeight = 0,
    decimal TotalVolumeCBM = 0);

public record AssignResourcesRequest(Guid VehicleId, Guid DriverId);
public record CancelTripRequest(string Reason);
public record AddStopRequest2(
    int Sequence, Guid OrderId, string Type,
    string? AddressName, string? AddressStreet, string? AddressProvince,
    double? Lat, double? Lng,
    DateTime? WindowFrom, DateTime? WindowTo);

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trips").WithTags("Dispatch/Trips");

        // POST /api/trips
        group.MapPost("/", async (CreateTripRequest req, ISender sender, CancellationToken ct) =>
        {
            var stops = req.Stops?.Select(s => new AddStopInput(
                s.Sequence, s.OrderId, s.Type, s.AddressName,
                s.AddressStreet, s.AddressProvince, s.Lat, s.Lng,
                s.WindowFrom, s.WindowTo)).ToList();

            var id = await sender.Send(new CreateTripCommand(
                req.PlannedDate, req.TenantId,
                req.TotalWeight, req.TotalVolumeCBM,
                null, stops), ct);
            return Results.Created($"/api/trips/{id}", new { Id = id });
        })
        .WithName("CreateTrip").WithSummary("สร้าง Trip ใหม่");

        // GET /api/trips
        group.MapGet("/", async (
            ISender sender,
            string? status = null, DateOnly? date = null,
            int page = 1, int pageSize = 20,
            Guid? tenantId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetTripsQuery(page, pageSize, status, date, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetTrips").WithSummary("รายการ Trips");

        // GET /api/trips/board
        group.MapGet("/board", async (
            ISender sender,
            DateOnly? date = null,
            Guid? tenantId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetDispatchBoardQuery(
                    date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    tenantId ?? Guid.Empty), ct);
            return Results.Ok(result);
        })
        .WithName("GetDispatchBoard").WithSummary("Dispatch Board (Timeline/Gantt)");

        // GET /api/trips/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTripByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetTripById").WithSummary("Trip Detail + Stops");

        // POST /api/trips/{id}/stops
        group.MapPost("/{id:guid}/stops", async (
            Guid id, AddStopRequest2 req, ISender sender, CancellationToken ct) =>
        {
            var stopId = await sender.Send(new AddStopCommand(id,
                new AddStopInput(req.Sequence, req.OrderId, req.Type,
                    req.AddressName, req.AddressStreet, req.AddressProvince,
                    req.Lat, req.Lng, req.WindowFrom, req.WindowTo)), ct);
            return Results.Created($"/api/trips/{id}/stops/{stopId}", new { Id = stopId });
        })
        .WithName("AddStop").WithSummary("เพิ่ม Stop เข้า Trip");

        // PUT /api/trips/{id}/assign
        group.MapPut("/{id:guid}/assign", async (
            Guid id, AssignResourcesRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AssignResourcesCommand(id, req.VehicleId, req.DriverId), ct);
            return Results.NoContent();
        })
        .WithName("AssignResources").WithSummary("จัดรถ + คนขับ");

        // PUT /api/trips/{id}/dispatch
        group.MapPut("/{id:guid}/dispatch", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DispatchTripCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DispatchTrip").WithSummary("ปล่อยงาน");

        // PUT /api/trips/{id}/cancel
        group.MapPut("/{id:guid}/cancel", async (
            Guid id, CancelTripRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CancelTripCommand(id, req.Reason), ct);
            return Results.NoContent();
        })
        .WithName("CancelTrip").WithSummary("ยกเลิก Trip");

        // PUT /api/trips/{id}/complete
        group.MapPut("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CompleteTripCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("CompleteTrip").WithSummary("จบ Trip");

        // PUT /api/trips/{id}/reassign
        group.MapPut("/{id:guid}/reassign", async (
            Guid id, AssignResourcesRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AssignResourcesCommand(id, req.VehicleId, req.DriverId), ct);
            return Results.NoContent();
        })
        .WithName("ReassignTrip").WithSummary("เปลี่ยนรถ/คนขับ");

        return app;
    }
}
