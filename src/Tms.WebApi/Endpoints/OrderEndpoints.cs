using MediatR;
using Tms.Orders.Application.Features.AmendOrder;
using Tms.Orders.Application.Features.CancelOrder;
using Tms.Orders.Application.Features.ConfirmOrder;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Application.Features.GetOrders;
using Tms.SharedKernel.Application;

namespace Tms.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // GET /api/orders — Admin, Planner, Dispatcher, Finance
        group.MapGet("/", async (
            ISender sender,
            ITenantContext tenant,
            int page = 1,
            int pageSize = 20,
            string? status = null,
            Guid? customerId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetOrdersQuery(page, pageSize, status, customerId, tenant.TenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrders")
        .WithSummary("ดูรายการ Orders")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner", "Dispatcher", "Finance"));

        // GET /api/orders/{id} — Admin, Planner, Dispatcher, Customer
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, ITenantContext tenant, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id, tenant.TenantId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetOrderById")
        .WithSummary("ดู Order ตาม ID")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner", "Dispatcher", "Customer"));

        // POST /api/orders — Admin, Planner, Customer
        group.MapPost("/", async (
            CreateOrderRequest request,
            ISender sender,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var command = new CreateOrderCommand(
                request.CustomerId,
                tenant.TenantId,
                request.OrderNumber,
                request.PickupAddress,
                request.DropoffAddress,
                request.Items,
                request.PickupWindow,
                request.DropoffWindow,
                request.Priority,
                request.Notes);

            var response = await sender.Send(command, ct);
            return Results.Created($"/api/orders/{response.Id}", response);
        })
        .WithName("CreateOrder")
        .WithSummary("สร้าง Order ใหม่")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner", "Customer"));

        // PUT /api/orders/{id}/confirm — Admin, Planner
        group.MapPut("/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ConfirmOrderCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("ConfirmOrder")
        .WithSummary("ยืนยัน Order")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner"));

        // PUT /api/orders/{id}/amend — Admin, Planner, Customer
        group.MapPut("/{id:guid}/amend", async (
            Guid id, AmendOrderRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AmendOrderCommand(id, request), ct);
            return Results.NoContent();
        })
        .WithName("AmendOrder")
        .WithSummary("แก้ไข Order (Draft/Confirmed)")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner", "Customer"));

        // PUT /api/orders/{id}/cancel — Admin, Planner, Customer
        group.MapPut("/{id:guid}/cancel", async (
            Guid id, CancelOrderRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new CancelOrderCommand(id, request.Reason), ct);
            return Results.NoContent();
        })
        .WithName("CancelOrder")
        .WithSummary("ยกเลิก Order")
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner", "Customer"));

        // POST /api/orders/import — Admin, Planner
        group.MapPost("/import", async (
            IFormFile file,
            ISender sender,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await sender.Send(
                new Tms.Orders.Application.Features.ImportOrder.ImportOrdersCommand(
                    stream, file.FileName, tenant.TenantId), ct);
            return Results.Ok(result);
        })
        .WithName("ImportOrders")
        .WithSummary("Bulk Import Orders (CSV/Excel)")
        .DisableAntiforgery()
        .RequireAuthorization(p => p.RequireRole("Admin", "Planner"));

        return app;
    }
}

public sealed record CancelOrderRequest(string Reason);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    string? OrderNumber,
    AddressDto PickupAddress,
    AddressDto DropoffAddress,
    List<OrderItemDto> Items,
    TimeWindowDto? PickupWindow = null,
    TimeWindowDto? DropoffWindow = null,
    string? Priority = "Normal",
    string? Notes = null
);
