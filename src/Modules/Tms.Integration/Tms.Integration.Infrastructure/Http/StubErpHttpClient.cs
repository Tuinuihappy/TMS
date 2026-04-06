using Tms.Integration.Application.Abstractions;

namespace Tms.Integration.Infrastructure.Http;

/// <summary>
/// Stub ERP HTTP Client — Phase 4.
/// ตอบ 200 เสมอพร้อม mock ERP Document Reference.
/// Phase 5+: แทนด้วย SAP/Oracle adapter จริง.
/// </summary>
public sealed class StubErpHttpClient : IErpHttpClient
{
    public Task<ErpExportResult> ExportArInvoiceAsync(
        string erpProviderCode, string payload, CancellationToken ct = default)
    {
        var docRef = $"ERP-AR-{erpProviderCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        return Task.FromResult(new ErpExportResult(IsSuccess: true, ErpDocumentRef: docRef, ErrorMessage: null));
    }

    public Task<ErpExportResult> ExportApCostAsync(
        string erpProviderCode, string payload, CancellationToken ct = default)
    {
        var docRef = $"ERP-AP-{erpProviderCode}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        return Task.FromResult(new ErpExportResult(IsSuccess: true, ErpDocumentRef: docRef, ErrorMessage: null));
    }
}
