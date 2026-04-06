using Tms.Documents.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Documents.Domain.Aggregates;

/// <summary>
/// Aggregate Root สำหรับไฟล์เอกสารที่เก็บใน Object Storage.
/// ใช้ Presigned URL Pattern: ไฟล์ไม่ผ่าน TMS Server โดยตรง.
/// </summary>
public sealed class StoredDocument : AggregateRoot
{
    public string FileName { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }

    /// <summary>Object Storage key / relative path (ไม่ใช่ URL)</summary>
    public string StoragePath { get; private set; } = default!;

    public DocumentCategory Category { get; private set; }
    public Guid OwnerId { get; private set; }
    public string OwnerType { get; private set; } = default!;
    public DocumentAccessLevel AccessLevel { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Guid UploadedBy { get; private set; }
    public Guid TenantId { get; private set; }

    // Retention in days per category
    private static readonly Dictionary<DocumentCategory, int?> RetentionDays = new()
    {
        [DocumentCategory.ProofOfDelivery] = 365 * 5,
        [DocumentCategory.Invoice]         = 365 * 7,
        [DocumentCategory.TripManifest]    = 365 * 2,
        [DocumentCategory.ImportFile]      = 90,
        [DocumentCategory.VehicleDoc]      = null,  // Manual delete
        [DocumentCategory.DriverLicense]   = null,
        [DocumentCategory.Other]           = null,
    };

    private StoredDocument() { }

    public static StoredDocument Create(
        string fileName,
        string contentType,
        long fileSizeBytes,
        string storagePath,
        DocumentCategory category,
        Guid ownerId,
        string ownerType,
        Guid uploadedBy,
        Guid tenantId)
    {
        var retentionDays = RetentionDays.GetValueOrDefault(category);
        return new StoredDocument
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            StoragePath = storagePath,
            Category = category,
            OwnerId = ownerId,
            OwnerType = ownerType,
            AccessLevel = DocumentAccessLevel.TenantInternal,
            IsDeleted = false,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy,
            TenantId = tenantId,
            ExpiresAt = retentionDays.HasValue
                ? DateTime.UtcNow.AddDays(retentionDays.Value)
                : null
        };
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
