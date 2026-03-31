using MediatR;
using Tms.Platform.Application.Features.MasterData;

namespace Tms.WebApi.Endpoints;

// ── Request DTOs ─────────────────────────────────────────────────────────
public record CreateCustomerRequest(
    string CustomerCode, string CompanyName, Guid TenantId,
    string? ContactPerson = null, string? Phone = null,
    string? Email = null, string? TaxId = null, string? PaymentTerms = null);

public record UpdateCustomerRequest(
    string CompanyName, string? ContactPerson, string? Phone,
    string? Email, string? TaxId, string? PaymentTerms);

public record CreateLocationRequest(
    string LocationCode, string Name, double Latitude, double Longitude,
    string Type, Guid TenantId,
    string? AddressLine = null, string? District = null,
    string? Province = null, string? PostalCode = null,
    string? Zone = null, Guid? CustomerId = null);

public record UpdateLocationRequest(
    string Name, string? AddressLine, string? Province,
    string? Zone, double Latitude, double Longitude);

public record CreateReasonCodeRequest(
    string Code, string Description, string Category, Guid TenantId);

public record CreateHolidayRequest(
    DateTime Date, string Description, Guid TenantId);

public static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/master").WithTags("Master Data");

        // ── Customers ───────────────────────────────────────────────────────

        group.MapPost("/customers", async (
            CreateCustomerRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateCustomerCommand(
                req.CustomerCode, req.CompanyName, req.TenantId,
                req.ContactPerson, req.Phone, req.Email, req.TaxId, req.PaymentTerms), ct);
            return Results.Created($"/api/master/customers/{id}", new { Id = id });
        })
        .WithName("CreateCustomer").WithSummary("สร้างลูกค้าใหม่");

        group.MapGet("/customers", async (
            ISender sender,
            int page = 1, int pageSize = 20, bool? isActive = null,
            Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetCustomersQuery(page, pageSize, isActive, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetCustomers").WithSummary("รายการลูกค้า (Paged)");

        group.MapGet("/customers/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCustomerByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetCustomerById").WithSummary("ข้อมูลลูกค้า");

        group.MapPut("/customers/{id:guid}", async (
            Guid id, UpdateCustomerRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateCustomerCommand(
                id, req.CompanyName, req.ContactPerson, req.Phone,
                req.Email, req.TaxId, req.PaymentTerms), ct);
            return Results.NoContent();
        })
        .WithName("UpdateCustomer").WithSummary("แก้ไขข้อมูลลูกค้า");

        group.MapDelete("/customers/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeactivateCustomerCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("DeactivateCustomer").WithSummary("ปิดใช้งานลูกค้า (Soft Delete)");

        // ── Locations ───────────────────────────────────────────────────────

        group.MapPost("/locations", async (
            CreateLocationRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(new CreateLocationCommand(
                req.LocationCode, req.Name, req.Latitude, req.Longitude,
                req.Type, req.TenantId, req.AddressLine, req.District,
                req.Province, req.PostalCode, req.Zone, req.CustomerId), ct);
            return Results.Created($"/api/master/locations/{id}", new { Id = id });
        })
        .WithName("CreateLocation").WithSummary("สร้าง Location");

        group.MapGet("/locations", async (
            ISender sender,
            string? type = null, string? zone = null, Guid? customerId = null,
            Guid? tenantId = null, int page = 1, int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetLocationsQuery(page, pageSize, type, zone, customerId, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetLocations").WithSummary("รายการ Locations (Paged, Filter)");

        group.MapGet("/locations/search", async (
            ISender sender, string q = "", string? type = null,
            Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new SearchLocationsQuery(q, type, tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("SearchLocations").WithSummary("ค้นหา Location (Autocomplete)");

        group.MapPut("/locations/{id:guid}", async (
            Guid id, UpdateLocationRequest req, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new UpdateLocationCommand(
                id, req.Name, req.AddressLine, req.Province,
                req.Zone, req.Latitude, req.Longitude), ct);
            return Results.NoContent();
        })
        .WithName("UpdateLocation").WithSummary("แก้ไข Location");

        // ── Reference Data ──────────────────────────────────────────────────

        group.MapGet("/reason-codes", async (
            ISender sender, string? category = null,
            Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetReasonCodesQuery(category, tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetReasonCodes").WithSummary("Reason Codes (Filter by Category)");

        group.MapPost("/reason-codes", async (
            CreateReasonCodeRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(
                new CreateReasonCodeCommand(req.Code, req.Description, req.Category, req.TenantId), ct);
            return Results.Created($"/api/master/reason-codes/{id}", new { Id = id });
        })
        .WithName("CreateReasonCode").WithSummary("สร้าง Reason Code");

        group.MapGet("/provinces", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProvincesQuery(), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetProvinces").WithSummary("รายการจังหวัดทั้งหมด");

        group.MapGet("/holidays", async (
            ISender sender, int year = 2026,
            Guid? tenantId = null, CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetHolidaysQuery(year, tenantId), ct);
            return Results.Ok(new { Year = year, Items = result });
        })
        .WithName("GetHolidays").WithSummary("รายการวันหยุด");

        group.MapPost("/holidays", async (
            CreateHolidayRequest req, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(
                new CreateHolidayCommand(req.Date, req.Description, req.TenantId), ct);
            return Results.Created($"/api/master/holidays/{id}", new { Id = id });
        })
        .WithName("CreateHoliday").WithSummary("สร้างวันหยุด");

        return app;
    }
}
