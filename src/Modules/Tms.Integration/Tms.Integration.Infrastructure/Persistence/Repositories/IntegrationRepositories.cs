using Microsoft.EntityFrameworkCore;
using Tms.Integration.Domain.Aggregates;
using Tms.Integration.Domain.Entities;
using Tms.Integration.Domain.Enums;
using Tms.Integration.Domain.Interfaces;
using Tms.Integration.Infrastructure.Persistence;

namespace Tms.Integration.Infrastructure.Persistence.Repositories;

public sealed class OmsSyncRepository(IntegrationDbContext db) : IOmsSyncRepository
{
    public Task<OmsOrderSync?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.OmsOrderSyncs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<OmsOrderSync>> GetPendingAsync(int batchSize, CancellationToken ct) =>
        db.OmsOrderSyncs
            .Where(x => x.Status == SyncStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public Task<List<OmsOrderSync>> GetFailedForRetryAsync(CancellationToken ct) =>
        db.OmsOrderSyncs
            .Where(x => x.Status == SyncStatus.Failed && x.NextRetryAt <= DateTime.UtcNow)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(string externalOrderRef, string omsProviderCode, CancellationToken ct) =>
        db.OmsOrderSyncs.AnyAsync(x =>
            x.ExternalOrderRef == externalOrderRef &&
            x.OmsProviderCode == omsProviderCode, ct);

    public async Task AddAsync(OmsOrderSync sync, CancellationToken ct)
    {
        db.OmsOrderSyncs.Add(sync);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OmsOrderSync sync, CancellationToken ct)
    {
        db.OmsOrderSyncs.Update(sync);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<OmsFieldMapping>> GetMappingsAsync(string omsProviderCode, CancellationToken ct) =>
        db.OmsFieldMappings.Where(x => x.OmsProviderCode == omsProviderCode).ToListAsync(ct);

    public async Task SaveMappingsAsync(string omsProviderCode, List<OmsFieldMapping> mappings, CancellationToken ct)
    {
        var existing = await db.OmsFieldMappings
            .Where(x => x.OmsProviderCode == omsProviderCode)
            .ToListAsync(ct);
        db.OmsFieldMappings.RemoveRange(existing);
        db.OmsFieldMappings.AddRange(mappings);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<OmsOutboxEvent>> GetPendingOutboxAsync(int batchSize, CancellationToken ct) =>
        db.OmsOutboxEvents
            .Where(x => x.Status == OutboxStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task AddOutboxEventAsync(OmsOutboxEvent outbox, CancellationToken ct)
    {
        db.OmsOutboxEvents.Add(outbox);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateOutboxEventAsync(OmsOutboxEvent outbox, CancellationToken ct)
    {
        db.OmsOutboxEvents.Update(outbox);
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> OutboxExistsAsync(string idempotencyKey, CancellationToken ct) =>
        db.OmsOutboxEvents.AnyAsync(x => x.IdempotencyKey == idempotencyKey, ct);
}

// ──────────────────────────────────────────────────────────────────────

public sealed class AmrHandoffRepository(IntegrationDbContext db) : IAmrHandoffRepository
{
    public Task<AmrHandoffRecord?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.AmrHandoffRecords.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<AmrHandoffRecord?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct) =>
        db.AmrHandoffRecords.FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, ct);

    public Task<bool> ExistsAsync(string amrJobId, string amrProviderCode, CancellationToken ct) =>
        db.AmrHandoffRecords.AnyAsync(x =>
            x.AmrJobId == amrJobId && x.AmrProviderCode == amrProviderCode, ct);

    public async Task AddAsync(AmrHandoffRecord record, CancellationToken ct)
    {
        db.AmrHandoffRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AmrHandoffRecord record, CancellationToken ct)
    {
        db.AmrHandoffRecords.Update(record);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<DockStation>> GetDocksAsync(Guid tenantId, CancellationToken ct) =>
        db.DockStations.Where(x => x.TenantId == tenantId && x.IsActive).ToListAsync(ct);

    public Task<DockStation?> GetDockByCodeAsync(string dockCode, CancellationToken ct) =>
        db.DockStations.FirstOrDefaultAsync(x => x.DockCode == dockCode, ct);

    public async Task UpdateDockAsync(DockStation dock, CancellationToken ct)
    {
        db.DockStations.Update(dock);
        await db.SaveChangesAsync(ct);
    }
}

// ──────────────────────────────────────────────────────────────────────

public sealed class ErpExportRepository(IntegrationDbContext db) : IErpExportRepository
{
    public Task<ErpExportJob?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.ErpExportJobs.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> HasOverlappingJobAsync(
        string erpProviderCode, ErpExportType type,
        DateOnly from, DateOnly to, CancellationToken ct) =>
        db.ErpExportJobs.AnyAsync(x =>
            x.ErpProviderCode == erpProviderCode &&
            x.ExportType == type &&
            x.Status != ExportJobStatus.Failed &&
            x.PeriodFrom <= to && x.PeriodTo >= from, ct);

    public async Task AddJobAsync(ErpExportJob job, CancellationToken ct)
    {
        db.ErpExportJobs.Add(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateJobAsync(ErpExportJob job, CancellationToken ct)
    {
        db.ErpExportJobs.Update(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddRecordAsync(ErpExportRecord record, CancellationToken ct)
    {
        db.ErpExportRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRecordAsync(ErpExportRecord record, CancellationToken ct)
    {
        db.ErpExportRecords.Update(record);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<ErpExportRecord>> GetFailedRecordsAsync(Guid jobId, CancellationToken ct) =>
        db.ErpExportRecords
            .Where(x => x.JobId == jobId && x.Status == RecordStatus.Rejected)
            .ToListAsync(ct);

    public async Task AddReconciliationAsync(ErpReconciliation r, CancellationToken ct)
    {
        db.ErpReconciliations.Add(r);
        await db.SaveChangesAsync(ct);
    }

    public Task<ErpReconciliation?> GetReconciliationByPaymentRefAsync(string paymentRef, CancellationToken ct) =>
        db.ErpReconciliations.FirstOrDefaultAsync(x => x.ErpPaymentRef == paymentRef, ct);

    public async Task UpdateReconciliationAsync(ErpReconciliation r, CancellationToken ct)
    {
        db.ErpReconciliations.Update(r);
        await db.SaveChangesAsync(ct);
    }
}
