using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Entities;
using Tms.Documents.Domain.Enums;
using Tms.Documents.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Documents.Application.Features.CreateUploadSession;

public sealed record CreateUploadSessionCommand(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DocumentCategory Category,
    Guid OwnerId,
    string OwnerType,
    Guid UploadedBy,
    Guid TenantId
) : ICommand<CreateUploadSessionResult>;

public sealed record CreateUploadSessionResult(
    Guid SessionId,
    string PresignedUploadUrl,
    DateTime ExpiresAt);

public sealed class CreateUploadSessionHandler(
    IDocumentRepository repo,
    IStorageProvider storage)
    : ICommandHandler<CreateUploadSessionCommand, CreateUploadSessionResult>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp",
        "application/pdf",
        "text/csv",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    ];

    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB

    public async Task<CreateUploadSessionResult> Handle(
        CreateUploadSessionCommand request, CancellationToken cancellationToken)
    {
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException($"Content type '{request.ContentType}' is not allowed.");

        if (request.FileSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File size exceeds the 50 MB limit.");

        var objectKey = storage.BuildObjectKey(request.TenantId, request.Category, request.FileName);
        var presignedUrl = await storage.GeneratePresignedUploadUrlAsync(objectKey, request.ContentType, cancellationToken);

        var session = UploadSession.Create(
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.Category,
            request.OwnerId,
            request.OwnerType,
            presignedUrl,
            request.UploadedBy,
            request.TenantId);

        await repo.AddSessionAsync(session, cancellationToken);

        return new CreateUploadSessionResult(session.Id, presignedUrl, session.ExpiresAt);
    }
}
