using MediatR;

namespace Tms.WebApi.Endpoints;

// ── Stub DTOs สำหรับ Trips ──────────────────────────────────────────────
public record CreateTripRequest(
    DateTime PlannedDate,
    List<AddStopRequest> Stops,
    decimal TotalWeight = 0,
    decimal TotalVolumeCBM = 0);

public record AddStopRequest(
    int Sequence,
    Guid OrderId,
    string Type, // Pickup | Dropoff | Return
    string? AddressName,
    string? AddressStreet,
    string? AddressProvince,
    double? Lat,
    double? Lng,
    DateTime? WindowFrom,
    DateTime? WindowTo);

public record AssignResourcesRequest(Guid VehicleId, Guid DriverId);
public record CancelTripRequest(string Reason);

public static class TripEndpoints
{
    public static IEndpointRouteBuilder MapTripEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trips").WithTags("Dispatch/Trips");

        // POST /api/trips
        group.MapPost("/", (CreateTripRequest request) =>
            Results.Accepted("/api/trips", new { Message = "Trip creation — coming in full implementation" }))
            .WithName("CreateTrip").WithSummary("สร้าง Trip ใหม่");

        // GET /api/trips
        group.MapGet("/", (string? status, DateOnly? date, int page = 1, int pageSize = 20) =>
            Results.Ok(new { Message = "Trip list — coming in full implementation", Status = status, Date = date }))
            .WithName("GetTrips").WithSummary("รายการ Trips");

        // GET /api/trips/board?date=
        group.MapGet("/board", (DateOnly? date) =>
            Results.Ok(new { Date = date?.ToString("yyyy-MM-dd"), Trips = Array.Empty<object>(), Summary = new { Total = 0, Dispatched = 0, Pending = 0 } }))
            .WithName("GetDispatchBoard").WithSummary("Dispatch Board (Timeline)");

        // GET /api/trips/{id}
        group.MapGet("/{id:guid}", (Guid id) =>
            Results.Ok(new { Id = id, Message = "Trip detail — coming in full implementation" }))
            .WithName("GetTripById").WithSummary("Trip Detail + Stops");

        // POST /api/trips/{id}/stops
        group.MapPost("/{id:guid}/stops", (Guid id, AddStopRequest stop) =>
            Results.Ok(new { TripId = id, Message = "Stop added" }))
            .WithName("AddStop").WithSummary("เพิ่ม Stop เข้า Trip");

        // PUT /api/trips/{id}/assign
        group.MapPut("/{id:guid}/assign", (Guid id, AssignResourcesRequest request) =>
            Results.Ok(new { Id = id, VehicleId = request.VehicleId, DriverId = request.DriverId, Status = "Assigned" }))
            .WithName("AssignResources").WithSummary("จัดรถ + คนขับ");

        // PUT /api/trips/{id}/dispatch
        group.MapPut("/{id:guid}/dispatch", (Guid id) =>
            Results.Ok(new { Id = id, Status = "Dispatched", DispatchedAt = DateTime.UtcNow }))
            .WithName("DispatchTrip").WithSummary("ปล่อยงาน");

        // PUT /api/trips/{id}/cancel
        group.MapPut("/{id:guid}/cancel", (Guid id, CancelTripRequest request) =>
            Results.NoContent())
            .WithName("CancelTrip").WithSummary("ยกเลิก Trip");

        // PUT /api/trips/{id}/reassign
        group.MapPut("/{id:guid}/reassign", (Guid id, AssignResourcesRequest request) =>
            Results.Ok(new { Id = id, Message = "Reassigned" }))
            .WithName("ReassignTrip").WithSummary("เปลี่ยนรถ/คนขับ");

        return app;
    }
}
