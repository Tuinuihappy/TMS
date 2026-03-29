using MediatR;

namespace Tms.WebApi.Endpoints;

public static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/master").WithTags("Master Data");

        // ── Customer ─────────────────────────────────────────────────────
        group.MapPost("/customers", () =>
            Results.Created("/api/master/customers/new", new { Message = "Customer created" }))
            .WithName("CreateCustomer").WithSummary("สร้างลูกค้าใหม่");

        group.MapGet("/customers", (int page = 1, int pageSize = 20) =>
            Results.Ok(new { Items = Array.Empty<object>(), Page = page, TotalCount = 0 }))
            .WithName("GetCustomers").WithSummary("รายการลูกค้า");

        group.MapGet("/customers/{id:guid}", (Guid id) =>
            Results.Ok(new { Id = id }))
            .WithName("GetCustomerById").WithSummary("ข้อมูลลูกค้า");

        group.MapPut("/customers/{id:guid}", (Guid id) =>
            Results.NoContent())
            .WithName("UpdateCustomer").WithSummary("แก้ไขข้อมูลลูกค้า");

        // ── Location ─────────────────────────────────────────────────────
        group.MapPost("/locations", () =>
            Results.Created("/api/master/locations/new", new { Message = "Location created" }))
            .WithName("CreateLocation").WithSummary("สร้าง Location");

        group.MapGet("/locations", (string? type, string? zone, int page = 1) =>
            Results.Ok(new { Items = Array.Empty<object>(), Page = page }))
            .WithName("GetLocations").WithSummary("รายการ Locations");

        group.MapGet("/locations/search", (string? q, string? type) =>
            Results.Ok(new { Items = Array.Empty<object>(), Query = q }))
            .WithName("SearchLocations").WithSummary("ค้นหา Location (Autocomplete)");

        group.MapPut("/locations/{id:guid}", (Guid id) =>
            Results.NoContent())
            .WithName("UpdateLocation").WithSummary("แก้ไข Location");

        // ── Reference Data ────────────────────────────────────────────────
        group.MapGet("/reason-codes", (string? category) =>
            Results.Ok(new { Items = Array.Empty<object>(), Category = category }))
            .WithName("GetReasonCodes").WithSummary("Reason Codes");

        group.MapPost("/reason-codes", () =>
            Results.Created("/api/master/reason-codes/new", new { Message = "Reason code created" }))
            .WithName("CreateReasonCode").WithSummary("สร้าง Reason Code");

        group.MapGet("/provinces", () =>
            Results.Ok(new { Items = Array.Empty<object>() }))
            .WithName("GetProvinces").WithSummary("รายการจังหวัด");

        group.MapGet("/holidays", (int year = 2026) =>
            Results.Ok(new { Year = year, Items = Array.Empty<object>() }))
            .WithName("GetHolidays").WithSummary("วันหยุด");

        return app;
    }
}
