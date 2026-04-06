using Tms.Documents.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Documents.Application.Features.GetDownloadUrl;

public sealed record GetDownloadUrlQuery(Guid DocumentId, Guid TenantId) : IQuery<GetDownloadUrlResult>;

public sealed record GetDownloadUrlResult(
    Guid DocumentId,
    string FileName,
    string DownloadUrl,
    DateTime UrlExpiresAt);

public sealed class GetDownloadUrlHandler(IDocumentRepository repo, IStorageProvider storage)
    : IQueryHandler<GetDownloadUrlQuery, GetDownloadUrlResult>
{
    public async Task<GetDownloadUrlResult> Handle(GetDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var doc = await repo.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document {request.DocumentId} not found.");

        if (doc.IsDeleted)
            throw new InvalidOperationException("Document has been deleted.");

        if (doc.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied to document from another tenant.");

        var downloadUrl = await storage.GeneratePresignedDownloadUrlAsync(doc.StoragePath, cancellationToken);

        return new GetDownloadUrlResult(
            DocumentId: doc.Id,
            FileName: doc.FileName,
            DownloadUrl: downloadUrl,
            UrlExpiresAt: DateTime.UtcNow.AddHours(1));
    }
}

// ──────────────────────────────────────────────────────────────────────

public sealed record GetDocumentsByOwnerQuery(
    Guid OwnerId,
    string OwnerType,
    Guid TenantId
) : IQuery<GetDocumentsByOwnerResult>;

public sealed record DocumentSummary(
    Guid Id,
    string FileName,
    string Category,
    string ContentType,
    long FileSizeBytes,
    DateTime UploadedAt);

public sealed record GetDocumentsByOwnerResult(
    Guid OwnerId,
    string OwnerType,
    List<DocumentSummary> Documents);

public sealed class GetDocumentsByOwnerHandler(IDocumentRepository repo)
    : IQueryHandler<GetDocumentsByOwnerQuery, GetDocumentsByOwnerResult>
{
    public async Task<GetDocumentsByOwnerResult> Handle(GetDocumentsByOwnerQuery request, CancellationToken cancellationToken)
    {
        var docs = await repo.GetByOwnerAsync(request.OwnerId, request.OwnerType, cancellationToken);

        var summaries = docs
            .Where(d => !d.IsDeleted && d.TenantId == request.TenantId)
            .Select(d => new DocumentSummary(d.Id, d.FileName, d.Category.ToString(), d.ContentType, d.FileSizeBytes, d.UploadedAt))
            .ToList();

        return new GetDocumentsByOwnerResult(request.OwnerId, request.OwnerType, summaries);
    }
}
