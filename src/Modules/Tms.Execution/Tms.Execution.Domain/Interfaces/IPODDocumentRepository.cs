using Tms.Execution.Domain.Entities;

namespace Tms.Execution.Domain.Interfaces;

public interface IPODDocumentRepository
{
    Task<PODDocument?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct = default);
    Task AddAsync(PODDocument document, CancellationToken ct = default);
    Task UpdateAsync(PODDocument document, CancellationToken ct = default);
}

/// <summary>
/// Abstraction for file storage — Phase 2 stub: local disk
/// Phase 3: swap to Azure Blob / AWS S3
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string blobUrl, CancellationToken ct = default);
}
