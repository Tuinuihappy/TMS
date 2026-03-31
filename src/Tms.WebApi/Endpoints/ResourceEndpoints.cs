using MediatR;
using Tms.Resources.Application.Features;

namespace Tms.WebApi.Endpoints;

public record CreateVehicleRequest(
    string PlateNumber, Guid VehicleTypeId, Guid TenantId,
    string Ownership = "Own", decimal CurrentOdometerKm = 0,
    DateTime? RegistrationExpiry = null, string? SubcontractorName = null);

public record ChangeStatusRequest(string Status, string? Reason = null);

public record AddMaintenanceRequest(
    string Type, DateTime ScheduledDate,
    decimal? OdometerAtService = null, string? Notes = null);

public record UpdateVehicleRequest(
    string? SubcontractorName = null,
    DateTime? RegistrationExpiry = null,
    decimal? CurrentOdometerKm = null);

public record CreateDriverRequest(
    string EmployeeCode, string FullName, Guid TenantId,
    string LicenseNumber, string LicenseType, DateTime LicenseExpiryDate,
    string? PhoneNumber = null);

public record UpdateDriverRequest(
    string? FullName = null,
    string? PhoneNumber = null,
    string? LicenseNumber = null,
    string? LicenseType = null,
    DateTime? LicenseExpiryDate = null);

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Vehicles ────────────────────────────────────────────────────────
        var vehicles = app.MapGroup("/api/vehicles").WithTags("Fleet/Vehicles");

        vehicles.MapPost("/", async (CreateVehicleRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateVehicleCommand(
                req.PlateNumber, req.VehicleTypeId, req.TenantId,
                req.Ownership, req.CurrentOdometerKm,
                req.RegistrationExpiry, req.SubcontractorName), ct);
            return Results.Created($"/api/vehicles/{id}", new { Id = id });
        })
        .WithName("CreateVehicle").WithSummary("ลงทะเบียนรถ");

        vehicles.MapGet("/", async (
            ISender sender,
            string? status = null, Guid? vehicleTypeId = null,
            Guid? tenantId = null, int page = 1, int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetVehiclesQuery(page, pageSize, status, vehicleTypeId, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetVehicles").WithSummary("รายการรถ");

        vehicles.MapGet("/available", async (
            ISender sender, Guid? tenantId = null, decimal? minPayloadKg = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetAvailableVehiclesQuery(tenantId ?? Guid.Empty, minPayloadKg), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetAvailableVehicles").WithSummary("รถที่พร้อมใช้ (Assignment)");

        vehicles.MapGet("/expiry-alerts", async (
            ISender sender, Guid? tenantId = null, int withinDays = 30,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetVehicleExpiryAlertsQuery(tenantId ?? Guid.Empty, withinDays), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetVehicleExpiryAlerts").WithSummary("รถที่ใกล้หมดอายุ");

        vehicles.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetVehicleByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetVehicleById").WithSummary("ข้อมูลรถ Detail");

        vehicles.MapPut("/{id:guid}", async (
            Guid id, UpdateVehicleRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateVehicleCommand(
                id, req.SubcontractorName, req.RegistrationExpiry, req.CurrentOdometerKm), ct);
            return Results.NoContent();
        })
        .WithName("UpdateVehicle").WithSummary("แก้ไขข้อมูลรถ");

        vehicles.MapPut("/{id:guid}/status", async (
            Guid id, ChangeStatusRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ChangeVehicleStatusCommand(id, req.Status), ct);
            return Results.NoContent();
        })
        .WithName("ChangeVehicleStatus").WithSummary("เปลี่ยนสถานะรถ");

        vehicles.MapPost("/{id:guid}/maintenance", async (
            Guid id, AddMaintenanceRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AddMaintenanceCommand(
                id, req.Type, req.ScheduledDate,
                req.OdometerAtService, req.Notes), ct);
            return Results.NoContent();
        })
        .WithName("AddMaintenance").WithSummary("บันทึกซ่อมบำรุง");

        // ── Drivers ─────────────────────────────────────────────────────────
        var drivers = app.MapGroup("/api/drivers").WithTags("Driver Management");

        drivers.MapPost("/", async (CreateDriverRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateDriverCommand(
                req.EmployeeCode, req.FullName, req.TenantId,
                req.LicenseNumber, req.LicenseType, req.LicenseExpiryDate,
                req.PhoneNumber), ct);
            return Results.Created($"/api/drivers/{id}", new { Id = id });
        })
        .WithName("CreateDriver").WithSummary("ลงทะเบียนคนขับ");

        drivers.MapGet("/", async (
            ISender sender,
            string? status = null, string? licenseType = null,
            Guid? tenantId = null, int page = 1, int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetDriversQuery(page, pageSize, status, licenseType, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDrivers").WithSummary("รายการคนขับ");

        drivers.MapGet("/available", async (
            ISender sender, Guid? tenantId = null, string? requiredLicenseType = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetAvailableDriversQuery(tenantId ?? Guid.Empty, requiredLicenseType), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetAvailableDrivers").WithSummary("คนขับพร้อมงาน");

        drivers.MapGet("/expiry-alerts", async (
            ISender sender, Guid? tenantId = null, int withinDays = 30,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetDriverExpiryAlertsQuery(tenantId ?? Guid.Empty, withinDays), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetDriverExpiryAlerts").WithSummary("ใบขับขี่ใกล้หมด");

        drivers.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDriverByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetDriverById").WithSummary("คนขับ Detail");

        drivers.MapPut("/{id:guid}", async (
            Guid id, UpdateDriverRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateDriverCommand(
                id, req.FullName, req.PhoneNumber,
                req.LicenseNumber, req.LicenseType, req.LicenseExpiryDate), ct);
            return Results.NoContent();
        })
        .WithName("UpdateDriver").WithSummary("แก้ไขข้อมูลคนขับ");

        drivers.MapPut("/{id:guid}/status", async (
            Guid id, ChangeStatusRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ChangeDriverStatusCommand(id, req.Status, req.Reason), ct);
            return Results.NoContent();
        })
        .WithName("ChangeDriverStatus").WithSummary("เปลี่ยนสถานะคนขับ");

        drivers.MapGet("/{id:guid}/hos", async (
            Guid id, ISender sender, DateOnly? date = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetDriverHOSQuery(id, date), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetDriverHOS").WithSummary("ประวัติ HOS คนขับ");

        return app;
    }
}
