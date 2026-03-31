using MediatR;
using Tms.Execution.Application.Features.GetShipments;
using Tms.Execution.Application.Features.UpdateShipmentStatus;

namespace Tms.WebApi.Endpoints;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipments").WithTags("Shipments");

        // GET /api/shipments/driver/today
        group.MapGet("/driver/today", async (
            ISender sender,
            Guid? driverId = null,
            Guid? tenantId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetDriverTodayShipmentsQuery(driverId ?? Guid.Empty, tenantId), ct);
            return Results.Ok(new { Items = result });
        })
        .WithName("GetDriverTodayShipments")
        .WithSummary("งานวันนี้ของ Driver");

        // GET /api/shipments
        group.MapGet("/", async (
            ISender sender,
            int page = 1, int pageSize = 20,
            string? status = null,
            Guid? tripId = null,
            Guid? tenantId = null,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(
                new GetShipmentsQuery(page, pageSize, status, tripId, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetShipments")
        .WithSummary("รายการ Shipments (Filter by status/trip)");

        // GET /api/shipments/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetShipmentByIdQuery(id), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .WithName("GetShipmentById")
        .WithSummary("Shipment Detail");

        // PUT /api/shipments/{id}/pickup
        group.MapPut("/{id:guid}/pickup", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new PickUpShipmentCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("PickUpShipment")
        .WithSummary("ยืนยัน Pickup (Driver)");

        // PUT /api/shipments/{id}/arrive
        group.MapPut("/{id:guid}/arrive", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ArriveShipmentCommand(id), ct);
            return Results.NoContent();
        })
        .WithName("ArriveShipment")
        .WithSummary("ยืนยันถึงจุดหมาย (Driver / Geofence)");

        // PUT /api/shipments/{id}/deliver
        group.MapPut("/{id:guid}/deliver", async (
            Guid id, DeliverRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DeliverShipmentCommand(
                id,
                request.Items.Select(i => new DeliveredItemDto(i.ShipmentItemId, i.DeliveredQty)).ToList(),
                new PodDto(request.Pod.ReceiverName, request.Pod.SignatureUrl,
                    request.Pod.PhotoUrls, request.Pod.Latitude, request.Pod.Longitude)), ct);
            return Results.NoContent();
        })
        .WithName("DeliverShipment")
        .WithSummary("ยืนยัน Deliver + POD (Driver)");

        // PUT /api/shipments/{id}/partial-deliver
        group.MapPut("/{id:guid}/partial-deliver", async (
            Guid id, DeliverRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new PartialDeliverShipmentCommand(
                id,
                request.Items.Select(i => new DeliveredItemDto(i.ShipmentItemId, i.DeliveredQty)).ToList(),
                new PodDto(request.Pod.ReceiverName, request.Pod.SignatureUrl,
                    request.Pod.PhotoUrls, request.Pod.Latitude, request.Pod.Longitude)), ct);
            return Results.NoContent();
        })
        .WithName("PartialDeliverShipment")
        .WithSummary("ส่งบางส่วน + POD (Driver)");

        // PUT /api/shipments/{id}/reject
        group.MapPut("/{id:guid}/reject", async (
            Guid id, RejectRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RejectShipmentCommand(id, request.Reason, request.ReasonCode), ct);
            return Results.NoContent();
        })
        .WithName("RejectShipment")
        .WithSummary("ตีกลับ + เหตุผล (Driver)");

        // PUT /api/shipments/{id}/exception
        group.MapPut("/{id:guid}/exception", async (
            Guid id, RejectRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new RecordShipmentExceptionCommand(id, request.Reason, request.ReasonCode), ct);
            return Results.NoContent();
        })
        .WithName("RecordShipmentException")
        .WithSummary("รายงานปัญหา (Driver / Dispatcher)");

        // PUT /api/shipments/{id}/pod/approve
        group.MapPut("/{id:guid}/pod/approve", async (
            Guid id, ApprovePodRequest request, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new ApprovePodCommand(id, request.ApprovedBy), ct);
            return Results.NoContent();
        })
        .WithName("ApprovePod")
        .WithSummary("อนุมัติ POD (Admin / Dispatcher)");

        return app;
    }
}

// ── Request DTOs ────────────────────────────────────────────────────────
public sealed record DeliverItemRequest(Guid ShipmentItemId, int DeliveredQty);
public sealed record PodRequest(
    string? ReceiverName,
    string? SignatureUrl,
    List<string>? PhotoUrls,
    double? Latitude,
    double? Longitude);
public sealed record DeliverRequest(List<DeliverItemRequest> Items, PodRequest Pod);
public sealed record RejectRequest(string Reason, string ReasonCode);
public sealed record ApprovePodRequest(Guid ApprovedBy);
