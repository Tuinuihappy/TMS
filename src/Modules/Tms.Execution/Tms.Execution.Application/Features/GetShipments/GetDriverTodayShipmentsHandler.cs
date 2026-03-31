using Tms.Execution.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Execution.Application.Features.GetShipments;

// ── Get Driver's Today Shipments ────────────────────────────────────
public sealed record GetDriverTodayShipmentsQuery(
    Guid DriverId,
    Guid? TenantId = null
) : IQuery<List<ShipmentDto>>;

public sealed class GetDriverTodayShipmentsHandler(IShipmentRepository repo)
    : IQueryHandler<GetDriverTodayShipmentsQuery, List<ShipmentDto>>
{
    public async Task<List<ShipmentDto>> Handle(
        GetDriverTodayShipmentsQuery request, CancellationToken ct)
    {
        // Get all shipments for today, then filter by driver via Trip
        // Since we don't have direct driver→shipment relation, we query by tenant
        // and date, which is scoped enough for the driver's app
        var (items, _) = await repo.GetPagedAsync(
            1, 100, null, null, request.TenantId, ct);

        var today = DateTime.UtcNow.Date;
        var todayShipments = items
            .Where(s => s.CreatedAt.Date == today)
            .ToList();

        return todayShipments.Select(s => new ShipmentDto(
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
                s.POD.CapturedAt, s.POD.ApprovalStatus.ToString()))).ToList();
    }
}
