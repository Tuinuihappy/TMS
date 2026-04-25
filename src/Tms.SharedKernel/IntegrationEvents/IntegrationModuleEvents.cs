using Tms.SharedKernel.Application;

namespace Tms.SharedKernel.IntegrationEvents;

public sealed record OrderSyncedFromOmsIntegrationEvent(
    Guid SyncId,
    string ExternalOrderRef,
    string OmsProviderCode,
    Guid TmsOrderId,
    string OrderNumber) : IntegrationEvent;

public sealed record DockReadyIntegrationEvent(
    string AmrJobId,
    string AmrProviderCode,
    string DockCode,
    Guid ShipmentId,
    int ItemsReady) : IntegrationEvent;

public sealed record InventoryHandoffConfirmedIntegrationEvent(
    Guid HandoffId,
    string AmrJobId,
    Guid ShipmentId,
    string DockCode,
    int ItemsExpected,
    int ItemsActual,
    string Status) : IntegrationEvent;

public sealed record ErpInvoiceExportedIntegrationEvent(
    Guid ExportRecordId,
    Guid InvoiceId,
    string InvoiceNumber,
    string ErpDocumentRef,
    string ErpProvider) : IntegrationEvent;

public sealed record PaymentReconciliationMatchedIntegrationEvent(
    Guid ReconciliationId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal AmountPaid,
    DateOnly PaidAt) : IntegrationEvent;

public sealed record DocumentUploadedIntegrationEvent(
    Guid DocumentId,
    string Category,
    Guid OwnerId,
    string OwnerType,
    string FileName,
    string ContentType,
    Guid TenantId) : IntegrationEvent;
