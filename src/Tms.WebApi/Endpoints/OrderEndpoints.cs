using MediatR;
using Tms.Orders.Application.Features.AmendOrder;
using Tms.Orders.Application.Features.CancelOrder;
using Tms.Orders.Application.Features.ConfirmOrder;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Application.Features.GetOrders;

namespace Tms.WebApi.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // GET /api/orders
        group.MapGet("/", async (
            ISender sender,
            int page = 1,
            int pageSize = 20,
            string? status = null,
            Guid? customerId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetOrdersQuery(page, pageSize, status, customerId), ct);
            return Results.Ok(result);
        })
        .WithName("GetOrders")
        .WithSummary("ดูรายการ Orders");

        // GET /api/orders/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetOrderById")
        .WithSummary("ดู Order ตาม ID");

        // POST /api/orders
        group.MapPost("/", async (CreateOrderCommand command, ISender sender, CancellationToken ct) =>
        {
            var id = await sender.Send(command, ct);
            return Results.Created($"/api/orders/{id}", new { Id = id });
        })
        .WithName("CreateOrder")
        .WithSummary("สร้าง Order ใหม่");

        // PUT /api/orders/{id}/confirm
        group.MapPut("/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ConfirmOrderCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("ConfirmOrder")
        .WithSummary("ยืนยัน Order");

        // PUT /api/orders/{id}/amend
        group.MapPut("/{id:guid}/amend", async (
            Guid id, AmendOrderRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new AmendOrderCommand(id, request), ct);
            return Results.NoContent();
        })
        .WithName("AmendOrder")
        .WithSummary("แก้ไข Order (Draft/Confirmed)");

        // PUT /api/orders/{id}/cancel
        group.MapPut("/{id:guid}/cancel", async (
            Guid id,
            CancelOrderRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new CancelOrderCommand(id, request.Reason), ct);
            return Results.NoContent();
        })
        .WithName("CancelOrder")
        .WithSummary("ยกเลิก Order");

        return app;
    }
}

public sealed record CancelOrderRequest(string Reason);
