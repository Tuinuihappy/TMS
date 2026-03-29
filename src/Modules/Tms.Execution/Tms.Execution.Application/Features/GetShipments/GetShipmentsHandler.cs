using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Application.Features.GetShipments;

// ── DTOs ────────────────────────────────────────────────────────────
public sealed record ShipmentItemDto(
    Guid Id,
    string? SKU,
    string Description,
    int ExpectedQty,
    int DeliveredQty,
    int ReturnedQty,
    string Status);

public sealed record PodResponseDto(
    string? ReceiverName,
    string? SignatureUrl,
    List<string> PhotoUrls,
    DateTime CapturedAt,
    string ApprovalStatus);

public sealed record ShipmentDto(
    Guid Id,
    string ShipmentNumber,
    Guid TripId,
    Guid OrderId,
    string Status,
    string? AddressName,
    string? AddressStreet,
    string? AddressProvince,
    string? ExceptionReason,
    string? ExceptionReasonCode,
    DateTime? PickedUpAt,
    DateTime? ArrivedAt,
    DateTime? DeliveredAt,
    DateTime CreatedAt,
    List<ShipmentItemDto> Items,
    PodResponseDto? Pod);

// ── Get List ─────────────────────────────────────────────────────────
public sealed record GetShipmentsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    Guid? TripId = null,
    Guid? TenantId = null
) : IQuery<PagedResult<ShipmentDto>>;

public sealed class GetShipmentsHandler(IShipmentRepository repo)
    : IQueryHandler<GetShipmentsQuery, PagedResult<ShipmentDto>>
{
    public async Task<PagedResult<ShipmentDto>> Handle(
        GetShipmentsQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.TripId, request.TenantId, ct);

        var dtos = items.Select(MapToDto).ToList();
        return PagedResult<ShipmentDto>.Create(dtos, total, request.Page, request.PageSize);
    }

    private static ShipmentDto MapToDto(Tms.Execution.Domain.Entities.Shipment s) => new(
        s.Id, s.ShipmentNumber, s.TripId, s.OrderId,
        s.Status.ToString(),
        s.AddressName, s.AddressStreet, s.AddressProvince,
        s.ExceptionReason, s.ExceptionReasonCode,
        s.PickedUpAt, s.ArrivedAt, s.DeliveredAt, s.CreatedAt,
        s.Items.Select(i => new ShipmentItemDto(
            i.Id, i.SKU, i.Description,
            i.ExpectedQty, i.DeliveredQty, i.ReturnedQty,
            i.Status.ToString())).ToList(),
        s.POD is null ? null : new PodResponseDto(
            s.POD.ReceiverName,
            s.POD.SignatureUrl,
            s.POD.Photos.Select(p => p.PhotoUrl).ToList(),
            s.POD.CapturedAt,
            s.POD.ApprovalStatus.ToString()));
}

// ── Get By Id ────────────────────────────────────────────────────────
public sealed record GetShipmentByIdQuery(Guid ShipmentId) : IQuery<ShipmentDto?>;

public sealed class GetShipmentByIdHandler(IShipmentRepository repo)
    : IQueryHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery request, CancellationToken ct)
    {
        var s = await repo.GetByIdAsync(request.ShipmentId, ct);
        if (s is null) return null;

        return new ShipmentDto(
            s.Id, s.ShipmentNumber, s.TripId, s.OrderId,
            s.Status.ToString(),
            s.AddressName, s.AddressStreet, s.AddressProvince,
            s.ExceptionReason, s.ExceptionReasonCode,
            s.PickedUpAt, s.ArrivedAt, s.DeliveredAt, s.CreatedAt,
            s.Items.Select(i => new ShipmentItemDto(
                i.Id, i.SKU, i.Description,
                i.ExpectedQty, i.DeliveredQty, i.ReturnedQty,
                i.Status.ToString())).ToList(),
            s.POD is null ? null : new PodResponseDto(
                s.POD.ReceiverName, s.POD.SignatureUrl,
                s.POD.Photos.Select(p => p.PhotoUrl).ToList(),
                s.POD.CapturedAt, s.POD.ApprovalStatus.ToString()));
    }
}
