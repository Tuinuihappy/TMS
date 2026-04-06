using System.Text.Json;
using Tms.Integration.Application.Abstractions;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.Features.ErpIntegration.ExportArInvoice;

public sealed record ExportArInvoiceCommand(
    string ErpProviderCode,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    Guid? CreatedBy,
    Guid TenantId
) : ICommand<Guid>;

public sealed class ExportArInvoiceHandler(
    IErpExportRepository repo,
    IErpHttpClient erpClient,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<ExportArInvoiceCommand, Guid>
{
    public async Task<Guid> Handle(ExportArInvoiceCommand request, CancellationToken cancellationToken)
    {
        // ป้องกัน Double Export
        if (await repo.HasOverlappingJobAsync(
            request.ErpProviderCode, ErpExportType.ArInvoice,
            request.PeriodFrom, request.PeriodTo, cancellationToken))
        {
            throw new InvalidOperationException(
                $"An AR Invoice export job already exists for {request.ErpProviderCode} " +
                $"covering period {request.PeriodFrom:yyyy-MM-dd} to {request.PeriodTo:yyyy-MM-dd}.");
        }

        var job = ErpExportJob.Create(
            request.ErpProviderCode,
            ErpExportType.ArInvoice,
            request.PeriodFrom,
            request.PeriodTo,
            request.CreatedBy,
            request.TenantId);

        await repo.AddJobAsync(job, cancellationToken);

        // MVP: ส่ง mock invoice payload ทันที (จริงๆ ควรใช้ Background Worker)
        // Phase 5: เปลี่ยนเป็น Queued pattern + Worker
        _ = Task.Run(async () =>
        {
            try
            {
                job.Start(recordsTotal: 1); // Stub — 1 record
                await repo.UpdateJobAsync(job, cancellationToken);

                var mockPayload = JsonSerializer.Serialize(new
                {
                    erpProvider = request.ErpProviderCode,
                    period = new { from = request.PeriodFrom, to = request.PeriodTo },
                    exportedAt = DateTime.UtcNow
                });

                var record = new ErpExportRecord
                {
                    JobId = job.Id,
                    SourceId = Guid.NewGuid(), // placeholder
                    SourceType = "Invoice",
                    Payload = mockPayload
                };
                await repo.AddRecordAsync(record, cancellationToken);

                var result = await erpClient.ExportArInvoiceAsync(request.ErpProviderCode, mockPayload, cancellationToken);

                if (result.IsSuccess)
                {
                    record.Status = RecordStatus.Accepted;
                    record.ErpDocumentRef = result.ErpDocumentRef;
                    record.AcceptedAt = DateTime.UtcNow;
                    job.RecordSuccess();

                    await publisher.PublishAsync(new ErpInvoiceExportedIntegrationEvent(
                        ExportRecordId: record.Id,
                        InvoiceId: record.SourceId,
                        InvoiceNumber: "INV-STUB",
                        ErpDocumentRef: result.ErpDocumentRef ?? "ERP-DOC-STUB",
                        ErpProvider: request.ErpProviderCode
                    ), cancellationToken);
                }
                else
                {
                    record.Status = RecordStatus.Rejected;
                    record.ErrorMessage = result.ErrorMessage;
                    job.RecordFailure();
                }

                await repo.UpdateRecordAsync(record, cancellationToken);
                job.Complete();
                await repo.UpdateJobAsync(job, cancellationToken);
            }
            catch (Exception)
            {
                job.Fail("Unexpected error during ERP export.");
                await repo.UpdateJobAsync(job, cancellationToken);
            }
        }, cancellationToken);

        return job.Id;
    }
}
