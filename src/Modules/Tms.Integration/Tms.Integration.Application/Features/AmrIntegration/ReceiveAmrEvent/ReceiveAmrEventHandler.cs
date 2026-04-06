using MediatR;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.Features.AmrIntegration.ReceiveAmrEvent;

public sealed record ReceiveAmrEventCommand(
    string AmrProviderCode,
    string EventType,
    string AmrJobId,
    string DockCode,
    Guid ShipmentId,
    int ItemsReady,
    string RawPayload,
    Guid TenantId
) : ICommand<Guid>;

public sealed class ReceiveAmrEventHandler(
    IAmrHandoffRepository repo,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<ReceiveAmrEventCommand, Guid>
{
    public async Task<Guid> Handle(ReceiveAmrEventCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: ถ้า Job นี้มีแล้วก็ return existing
        if (await repo.ExistsAsync(request.AmrJobId, request.AmrProviderCode, cancellationToken))
            return Guid.Empty;

        var record = AmrHandoffRecord.Create(
            request.AmrJobId,
            request.AmrProviderCode,
            request.ShipmentId,
            request.DockCode,
            request.ItemsReady,
            request.RawPayload,
            request.TenantId);

        // เมื่อรับ DOCK_READY event → เปลี่ยนสถานะเป็น AmrAtDock
        if (request.EventType.Equals("DOCK_READY", StringComparison.OrdinalIgnoreCase))
            record.MarkAmrAtDock();

        await repo.AddAsync(record, cancellationToken);

        // อัปเดต Dock status
        var dock = await repo.GetDockByCodeAsync(request.DockCode, cancellationToken);
        if (dock is not null)
        {
            dock.Status = DockStatus.Occupied;
            await repo.UpdateDockAsync(dock, cancellationToken);
        }

        // แจ้ง Execution module ว่า AMR พร้อมที่ Dock แล้ว
        await publisher.PublishAsync(new DockReadyIntegrationEvent(
            AmrJobId: request.AmrJobId,
            AmrProviderCode: request.AmrProviderCode,
            DockCode: request.DockCode,
            ShipmentId: request.ShipmentId,
            ItemsReady: request.ItemsReady
        ), cancellationToken);

        return record.Id;
    }
}
