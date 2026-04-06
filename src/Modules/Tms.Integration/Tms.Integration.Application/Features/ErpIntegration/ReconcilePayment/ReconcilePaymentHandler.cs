using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Integration.Application.Features.ErpIntegration.ReconcilePayment;

public sealed record PaymentItem(
    string ErpPaymentRef,
    string InvoiceNumber,
    decimal AmountPaid,
    string Currency,
    DateOnly PaidAt);

public sealed record ReconcilePaymentCommand(
    List<PaymentItem> Payments,
    Guid TenantId
) : ICommand<ReconcilePaymentResult>;

public sealed record ReconcilePaymentResult(int Processed, int Matched, int Unmatched);

public sealed class ReconcilePaymentHandler(
    IErpExportRepository repo,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<ReconcilePaymentCommand, ReconcilePaymentResult>
{
    public async Task<ReconcilePaymentResult> Handle(ReconcilePaymentCommand request, CancellationToken cancellationToken)
    {
        int matched = 0, unmatched = 0;

        foreach (var payment in request.Payments)
        {
            // Idempotency — ข้ามถ้า Payment Ref นี้มีแล้ว
            var existing = await repo.GetReconciliationByPaymentRefAsync(payment.ErpPaymentRef, cancellationToken);
            if (existing is not null) continue;

            var reconciliation = new ErpReconciliation
            {
                ErpPaymentRef = payment.ErpPaymentRef,
                InvoiceNumber = payment.InvoiceNumber,
                AmountPaid = payment.AmountPaid,
                Currency = payment.Currency,
                PaidAt = payment.PaidAt,
                TenantId = request.TenantId
            };

            // MVP: ถือว่า Match เสมอ (Billing module lookup จะทำใน Phase จริง)
            reconciliation.Status = ReconciliationStatus.Matched;
            reconciliation.InvoiceId = Guid.NewGuid(); // placeholder

            await repo.AddReconciliationAsync(reconciliation, cancellationToken);

            if (reconciliation.Status == ReconciliationStatus.Matched)
            {
                matched++;
                await publisher.PublishAsync(new PaymentReconciliationMatchedIntegrationEvent(
                    ReconciliationId: reconciliation.Id,
                    InvoiceId: reconciliation.InvoiceId!.Value,
                    InvoiceNumber: reconciliation.InvoiceNumber,
                    AmountPaid: reconciliation.AmountPaid,
                    PaidAt: reconciliation.PaidAt
                ), cancellationToken);
            }
            else
            {
                unmatched++;
            }
        }

        return new ReconcilePaymentResult(request.Payments.Count, matched, unmatched);
    }
}
