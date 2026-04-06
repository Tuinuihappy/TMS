using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Entities;
using Tms.Documents.Domain.Enums;

namespace Tms.Documents.Domain.Interfaces;

public interface IDocumentRepository
{
    Task<StoredDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<StoredDocument>> GetByOwnerAsync(Guid ownerId, string ownerType, CancellationToken ct = default);
    Task AddAsync(StoredDocument document, CancellationToken ct = default);
    Task UpdateAsync(StoredDocument document, CancellationToken ct = default);

    Task<UploadSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task AddSessionAsync(UploadSession session, CancellationToken ct = default);
    Task UpdateSessionAsync(UploadSession session, CancellationToken ct = default);
}

/// <summary>
/// Interface สำหรับ Object Storage — สามารถ swap เป็น S3/Azure Blob ได้โดยไม่แตะ Domain/Application.
/// </summary>
public interface IStorageProvider
{
    /// <summary>สร้าง Presigned URL สำหรับ PUT upload (15 นาที)</summary>
    Task<string> GeneratePresignedUploadUrlAsync(string objectKey, string contentType, CancellationToken ct = default);

    /// <summary>สร้าง Presigned URL สำหรับ GET download (1 ชั่วโมง)</summary>
    Task<string> GeneratePresignedDownloadUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>ลบไฟล์จาก Storage (Hard delete)</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>สร้าง Object Key จาก metadata</summary>
    string BuildObjectKey(Guid tenantId, DocumentCategory category, string fileName);
}
