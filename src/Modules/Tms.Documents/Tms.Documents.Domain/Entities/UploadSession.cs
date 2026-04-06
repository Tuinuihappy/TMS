using Tms.Documents.Domain.Enums;
using Tms.SharedKernel.Domain;

namespace Tms.Documents.Domain.Entities;

public sealed class UploadSession : BaseEntity
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public DocumentCategory Category { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerType { get; set; } = default!;
    public string PresignedUploadUrl { get; set; } = default!;
    public UploadSessionStatus Status { get; set; } = UploadSessionStatus.Active;
    public Guid? LinkedDocumentId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    public Guid TenantId { get; set; }

    public static UploadSession Create(
        string fileName,
        string contentType,
        long fileSizeBytes,
        DocumentCategory category,
        Guid ownerId,
        string ownerType,
        string presignedUrl,
        Guid createdBy,
        Guid tenantId)
    {
        return new UploadSession
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            Category = category,
            OwnerId = ownerId,
            OwnerType = ownerType,
            PresignedUploadUrl = presignedUrl,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedBy = createdBy,
            TenantId = tenantId
        };
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public void Complete(Guid documentId)
    {
        Status = UploadSessionStatus.Completed;
        LinkedDocumentId = documentId;
    }
}
