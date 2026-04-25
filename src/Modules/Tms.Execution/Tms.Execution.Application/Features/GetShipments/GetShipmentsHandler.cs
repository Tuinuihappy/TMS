using Microsoft.EntityFrameworkCore;
using Tms.Execution.Application.Common.Interfaces;
using Tms.Execution.Domain.Enums;
using Tms.SharedKernel.Application;

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
    Guid DropoffStopId,
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

public sealed class GetShipmentsHandler(IExecutionDbContext db)
    : IQueryHandler<GetShipmentsQuery, PagedResult<ShipmentDto>>
{
    public async Task<PagedResult<ShipmentDto>> Handle(
        GetShipmentsQuery request, CancellationToken ct)
    {
        var query = db.Shipments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<ShipmentStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            query = query.Where(s => s.Status == parsedStatus);

        if (request.TripId.HasValue)
            query = query.Where(s => s.TripId == request.TripId.Value);

        if (request.TenantId.HasValue)
            query = query.Where(s => s.TenantId == request.TenantId.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ShipmentDto(
                s.Id, s.ShipmentNumber, s.TripId, s.OrderId, s.DropoffStopId,
                s.Status.ToString(),
                s.AddressName, s.AddressStreet, s.AddressProvince,
                s.ExceptionReason, s.ExceptionReasonCode,
                s.PickedUpAt, s.ArrivedAt, s.DeliveredAt, s.CreatedAt,
                s.Items.Select(i => new ShipmentItemDto(
                    i.Id, i.SKU, i.Description,
                    i.ExpectedQty, i.DeliveredQty, i.ReturnedQty,
                    i.Status.ToString())).ToList(),
                s.POD != null
                    ? new PodResponseDto(
                        s.POD.ReceiverName, s.POD.SignatureUrl,
                        s.POD.Photos.Select(p => p.PhotoUrl).ToList(),
                        s.POD.CapturedAt, s.POD.ApprovalStatus.ToString())
                    : null))
            .ToListAsync(ct);

        return PagedResult<ShipmentDto>.Create(items, total, request.Page, request.PageSize);
    }
}

// ── Get By Id ────────────────────────────────────────────────────────
public sealed record GetShipmentByIdQuery(Guid ShipmentId) : IQuery<ShipmentDto?>;

public sealed class GetShipmentByIdHandler(IExecutionDbContext db)
    : IQueryHandler<GetShipmentByIdQuery, ShipmentDto?>
{
    public async Task<ShipmentDto?> Handle(GetShipmentByIdQuery request, CancellationToken ct) =>
        await db.Shipments
            .AsNoTracking()
            .Where(s => s.Id == request.ShipmentId)
            .Select(s => new ShipmentDto(
                s.Id, s.ShipmentNumber, s.TripId, s.OrderId, s.DropoffStopId,
                s.Status.ToString(),
                s.AddressName, s.AddressStreet, s.AddressProvince,
                s.ExceptionReason, s.ExceptionReasonCode,
                s.PickedUpAt, s.ArrivedAt, s.DeliveredAt, s.CreatedAt,
                s.Items.Select(i => new ShipmentItemDto(
                    i.Id, i.SKU, i.Description,
                    i.ExpectedQty, i.DeliveredQty, i.ReturnedQty,
                    i.Status.ToString())).ToList(),
                s.POD != null
                    ? new PodResponseDto(
                        s.POD.ReceiverName, s.POD.SignatureUrl,
                        s.POD.Photos.Select(p => p.PhotoUrl).ToList(),
                        s.POD.CapturedAt, s.POD.ApprovalStatus.ToString())
                    : null))
            .FirstOrDefaultAsync(ct);
}
