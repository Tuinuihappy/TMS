using MediatR;

namespace Tms.WebApi.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder app)
    {
        var vehicleGroup = app.MapGroup("/api/vehicles").WithTags("Resources");

        vehicleGroup.MapGet("/", () => Results.Ok(new { Message = "Vehicle list — coming soon in Phase 1" }))
            .WithName("GetVehicles").WithSummary("ดูรายการรถ");

        var driverGroup = app.MapGroup("/api/drivers").WithTags("Resources");

        driverGroup.MapGet("/", () => Results.Ok(new { Message = "Driver list — coming soon in Phase 1" }))
            .WithName("GetDrivers").WithSummary("ดูรายการคนขับ");

        return app;
    }
}
