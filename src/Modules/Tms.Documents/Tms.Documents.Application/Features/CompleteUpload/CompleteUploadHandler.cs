using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Documents.Application.Features.CompleteUpload;

public sealed record CompleteUploadCommand(
    Guid SessionId,
    Guid UploadedBy,
    Guid TenantId
) : ICommand<Guid>;

public sealed class CompleteUploadHandler(
    IDocumentRepository repo,
    IStorageProvider storage,
    IIntegrationEventPublisher publisher)
    : ICommandHandler<CompleteUploadCommand, Guid>
{
    public async Task<Guid> Handle(CompleteUploadCommand request, CancellationToken cancellationToken)
    {
        var session = await repo.GetSessionByIdAsync(request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Upload session {request.SessionId} not found.");

        if (session.IsExpired)
            throw new InvalidOperationException("Upload session has expired. Please create a new session.");

        if (session.Status == Domain.Enums.UploadSessionStatus.Completed)
            return session.LinkedDocumentId!.Value; // Idempotent

        // สร้าง StoredDocument — object key คือ path ที่ file ถูก upload ไป
        var objectKey = storage.BuildObjectKey(request.TenantId, session.Category, session.FileName);

        var document = StoredDocument.Create(
            session.FileName,
            session.ContentType,
            session.FileSizeBytes,
            objectKey,
            session.Category,
            session.OwnerId,
            session.OwnerType,
            request.UploadedBy,
            request.TenantId);

        await repo.AddAsync(document, cancellationToken);

        session.Complete(document.Id);
        await repo.UpdateSessionAsync(session, cancellationToken);

        // แจ้ง Module อื่น (POD, Notification)
        await publisher.PublishAsync(new DocumentUploadedIntegrationEvent(
            DocumentId: document.Id,
            Category: document.Category.ToString(),
            OwnerId: document.OwnerId,
            OwnerType: document.OwnerType,
            FileName: document.FileName,
            ContentType: document.ContentType,
            TenantId: document.TenantId
        ), cancellationToken);

        return document.Id;
    }
}
