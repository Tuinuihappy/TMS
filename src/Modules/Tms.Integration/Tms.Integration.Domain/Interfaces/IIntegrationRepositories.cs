using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;

namespace Tms.Integration.Domain.Interfaces;

public interface IOmsSyncRepository
{
    Task<OmsOrderSync?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<OmsOrderSync>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    Task<List<OmsOrderSync>> GetFailedForRetryAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string externalOrderRef, string omsProviderCode, CancellationToken ct = default);
    Task AddAsync(OmsOrderSync sync, CancellationToken ct = default);
    Task UpdateAsync(OmsOrderSync sync, CancellationToken ct = default);

    Task<List<OmsFieldMapping>> GetMappingsAsync(string omsProviderCode, CancellationToken ct = default);
    Task SaveMappingsAsync(string omsProviderCode, List<OmsFieldMapping> mappings, CancellationToken ct = default);

    Task<List<OmsOutboxEvent>> GetPendingOutboxAsync(int batchSize, CancellationToken ct = default);
    Task AddOutboxEventAsync(OmsOutboxEvent outbox, CancellationToken ct = default);
    Task UpdateOutboxEventAsync(OmsOutboxEvent outbox, CancellationToken ct = default);
    Task<bool> OutboxExistsAsync(string idempotencyKey, CancellationToken ct = default);
}

public interface IAmrHandoffRepository
{
    Task<AmrHandoffRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AmrHandoffRecord?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string amrJobId, string amrProviderCode, CancellationToken ct = default);
    Task AddAsync(AmrHandoffRecord record, CancellationToken ct = default);
    Task UpdateAsync(AmrHandoffRecord record, CancellationToken ct = default);

    Task<List<DockStation>> GetDocksAsync(Guid tenantId, CancellationToken ct = default);
    Task<DockStation?> GetDockByCodeAsync(string dockCode, CancellationToken ct = default);
    Task UpdateDockAsync(DockStation dock, CancellationToken ct = default);
}

public interface IErpExportRepository
{
    Task<ErpExportJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasOverlappingJobAsync(string erpProviderCode, ErpExportType type, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task AddJobAsync(ErpExportJob job, CancellationToken ct = default);
    Task UpdateJobAsync(ErpExportJob job, CancellationToken ct = default);
    Task AddRecordAsync(ErpExportRecord record, CancellationToken ct = default);
    Task UpdateRecordAsync(ErpExportRecord record, CancellationToken ct = default);
    Task<List<ErpExportRecord>> GetFailedRecordsAsync(Guid jobId, CancellationToken ct = default);

    Task AddReconciliationAsync(ErpReconciliation reconciliation, CancellationToken ct = default);
    Task<ErpReconciliation?> GetReconciliationByPaymentRefAsync(string paymentRef, CancellationToken ct = default);
    Task UpdateReconciliationAsync(ErpReconciliation reconciliation, CancellationToken ct = default);
}
