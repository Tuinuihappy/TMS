namespace Tms.Integration.Application.Abstractions;

/// <summary>
/// Interface สำหรับ ERP HTTP Client.
/// Phase 4: Stub ตอบ 200 เสมอ. Phase 5+: แทนด้วย SAP/Oracle adapter จริง.
/// </summary>
public interface IErpHttpClient
{
    Task<ErpExportResult> ExportArInvoiceAsync(string erpProviderCode, string payload, CancellationToken ct = default);
    Task<ErpExportResult> ExportApCostAsync(string erpProviderCode, string payload, CancellationToken ct = default);
}

public sealed record ErpExportResult(
    bool IsSuccess,
    string? ErpDocumentRef,
    string? ErrorMessage);
