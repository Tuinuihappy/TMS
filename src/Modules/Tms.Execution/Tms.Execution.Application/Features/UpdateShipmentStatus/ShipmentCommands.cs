using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Application.Features.UpdateShipmentStatus;

// ── DTOs shared across commands ──────────────────────────────────────
public sealed record DeliveredItemDto(Guid ShipmentItemId, int DeliveredQty);

public sealed record PodDto(
    string? ReceiverName,
    string? SignatureUrl,
    List<string>? PhotoUrls,
    double? Latitude,
    double? Longitude);

// ── PickUp ───────────────────────────────────────────────────────────
public sealed record PickUpShipmentCommand(Guid ShipmentId) : ICommand;

public sealed class PickUpShipmentHandler(IShipmentRepository repo)
    : ICommandHandler<PickUpShipmentCommand>
{
    public async Task Handle(PickUpShipmentCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        shipment.PickUp();
        await repo.UpdateAsync(shipment, ct);
    }
}

// ── Arrive ───────────────────────────────────────────────────────────
public sealed record ArriveShipmentCommand(Guid ShipmentId) : ICommand;

public sealed class ArriveShipmentHandler(IShipmentRepository repo)
    : ICommandHandler<ArriveShipmentCommand>
{
    public async Task Handle(ArriveShipmentCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        shipment.Arrive();
        await repo.UpdateAsync(shipment, ct);
    }
}

// ── Deliver ──────────────────────────────────────────────────────────
public sealed record DeliverShipmentCommand(
    Guid ShipmentId,
    List<DeliveredItemDto> Items,
    PodDto Pod) : ICommand;

public sealed class DeliverShipmentHandler(IShipmentRepository repo)
    : ICommandHandler<DeliverShipmentCommand>
{
    public async Task Handle(DeliverShipmentCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        var pod = CreatePod(shipment.Id, request.Pod);

        // Add POD record directly (avoids EF treating new POD as modification)
        await repo.AddPodRecordAsync(pod, ct);

        // Re-load shipment so it picks up the POD
        shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        var items = request.Items.Select(i => (i.ShipmentItemId, i.DeliveredQty));
        shipment.Deliver(items, shipment.POD!);
        await repo.UpdateAsync(shipment, ct);
    }

    private static PODRecord CreatePod(Guid shipmentId, PodDto dto)
    {
        var pod = PODRecord.Create(
            shipmentId, dto.ReceiverName, dto.SignatureUrl,
            dto.Latitude, dto.Longitude);
        foreach (var url in dto.PhotoUrls ?? [])
            pod.AddPhoto(url);
        return pod;
    }
}

// ── Partial Deliver ───────────────────────────────────────────────────
public sealed record PartialDeliverShipmentCommand(
    Guid ShipmentId,
    List<DeliveredItemDto> Items,
    PodDto Pod) : ICommand;

public sealed class PartialDeliverShipmentHandler(IShipmentRepository repo)
    : ICommandHandler<PartialDeliverShipmentCommand>
{
    public async Task Handle(PartialDeliverShipmentCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        var pod = PODRecord.Create(
            shipment.Id, request.Pod.ReceiverName, request.Pod.SignatureUrl,
            request.Pod.Latitude, request.Pod.Longitude);
        foreach (var url in request.Pod.PhotoUrls ?? [])
            pod.AddPhoto(url);

        await repo.AddPodRecordAsync(pod, ct);

        shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);

        var items = request.Items.Select(i => (i.ShipmentItemId, i.DeliveredQty));
        shipment.PartialDeliver(items, shipment.POD!);
        await repo.UpdateAsync(shipment, ct);
    }
}

// ── Reject ───────────────────────────────────────────────────────────
public sealed record RejectShipmentCommand(
    Guid ShipmentId,
    string Reason,
    string ReasonCode) : ICommand;

public sealed class RejectShipmentHandler(IShipmentRepository repo)
    : ICommandHandler<RejectShipmentCommand>
{
    public async Task Handle(RejectShipmentCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        shipment.Reject(request.Reason, request.ReasonCode);
        await repo.UpdateAsync(shipment, ct);
    }
}

// ── Record Exception ─────────────────────────────────────────────────
public sealed record RecordShipmentExceptionCommand(
    Guid ShipmentId,
    string Reason,
    string ReasonCode) : ICommand;

public sealed class RecordShipmentExceptionHandler(IShipmentRepository repo)
    : ICommandHandler<RecordShipmentExceptionCommand>
{
    public async Task Handle(RecordShipmentExceptionCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        shipment.RecordException(request.Reason, request.ReasonCode);
        await repo.UpdateAsync(shipment, ct);
    }
}

// ── Approve POD ──────────────────────────────────────────────────────
public sealed record ApprovePodCommand(Guid ShipmentId, Guid ApprovedBy) : ICommand;

public sealed class ApprovePodHandler(IShipmentRepository repo)
    : ICommandHandler<ApprovePodCommand>
{
    public async Task Handle(ApprovePodCommand request, CancellationToken ct)
    {
        var shipment = await repo.GetByIdAsync(request.ShipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), request.ShipmentId);
        shipment.ApprovePOD(request.ApprovedBy);
        await repo.UpdateAsync(shipment, ct);
    }
}
