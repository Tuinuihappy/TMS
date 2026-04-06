using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.Features.AmrIntegration.ConfirmHandoff;

public sealed record ConfirmHandoffCommand(
    Guid HandoffId,
    int ItemsActual,
    string? DriverNote
) : ICommand;

public sealed class ConfirmHandoffHandler(
    IAmrHandoffRepository repo,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<ConfirmHandoffCommand>
{
    public async Task Handle(ConfirmHandoffCommand request, CancellationToken cancellationToken)
    {
        var record = await repo.GetByIdAsync(request.HandoffId, cancellationToken)
            ?? throw new KeyNotFoundException($"HandoffRecord {request.HandoffId} not found.");

        // ถ้ายัง AmrAtDock → เปลี่ยนเป็น Transferring ก่อน
        if (record.Status == HandoffStatus.AmrAtDock)
            record.StartTransfer();

        record.ConfirmHandoff(request.ItemsActual, request.DriverNote);
        await repo.UpdateAsync(record, cancellationToken);

        // คืน Dock
        var dock = await repo.GetDockByCodeAsync(record.DockCode, cancellationToken);
        if (dock is not null)
        {
            dock.Status = DockStatus.Available;
            dock.AssignedVehicleId = null;
            await repo.UpdateDockAsync(dock, cancellationToken);
        }

        // แจ้ง Execution ให้อัปเดต Shipment
        await publisher.PublishAsync(new InventoryHandoffConfirmedIntegrationEvent(
            HandoffId: record.Id,
            AmrJobId: record.AmrJobId,
            ShipmentId: record.ShipmentId,
            DockCode: record.DockCode,
            ItemsExpected: record.ItemsExpected,
            ItemsActual: request.ItemsActual,
            Status: record.Status.ToString()
        ), cancellationToken);
    }
}
