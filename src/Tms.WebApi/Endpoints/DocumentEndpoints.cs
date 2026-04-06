using MediatR;
using Tms.Documents.Application.Features.CompleteUpload;
using Tms.Documents.Application.Features.CreateUploadSession;
using Tms.Documents.Application.Features.GetDownloadUrl;
using Tms.Documents.Domain.Enums;
using Tms.Documents.Infrastructure.Storage;

namespace Tms.WebApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        // POST /api/documents/upload-session
        group.MapPost("/upload-session", async (
            CreateUploadSessionRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<DocumentCategory>(request.Category, true, out var category))
                return Results.BadRequest(new { error = $"Invalid category '{request.Category}'." });

            var result = await sender.Send(new CreateUploadSessionCommand(
                FileName: request.FileName,
                ContentType: request.ContentType,
                FileSizeBytes: request.FileSizeBytes,
                Category: category,
                OwnerId: request.OwnerId,
                OwnerType: request.OwnerType,
                UploadedBy: Guid.Empty, // TODO: จาก JWT claim
                TenantId: Guid.Empty    // TODO: จาก JWT claim
            ), ct);

            return Results.Created($"/api/documents/upload-session/{result.SessionId}", new
            {
                sessionId = result.SessionId,
                presignedUploadUrl = result.PresignedUploadUrl,
                uploadMethod = "POST",
                expiresAt = result.ExpiresAt
            });
        })
        .WithName("CreateUploadSession")
        .WithSummary("สร้าง Upload Session + Presigned URL");

        // POST /api/documents/upload-session/{sessionId}/complete
        group.MapPost("/upload-session/{sessionId:guid}/complete", async (
            Guid sessionId,
            ISender sender,
            CancellationToken ct) =>
        {
            var documentId = await sender.Send(new CompleteUploadCommand(
                SessionId: sessionId,
                UploadedBy: Guid.Empty,
                TenantId: Guid.Empty
            ), ct);

            return Results.Created($"/api/documents/{documentId}", new { documentId });
        })
        .WithName("CompleteUpload")
        .WithSummary("ยืนยันว่า Upload เสร็จแล้ว → สร้าง StoredDocument");

        // POST /api/documents/local-upload/{objectKey} (Dev only — Local Storage stub)
        group.MapPost("/local-upload/{*objectKey}", async (
            string objectKey,
            IFormFile file,
            LocalFileStorageProvider storage,
            CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var savedKey = await storage.SaveLocalFileAsync(objectKey, stream, ct);
            return Results.Ok(new { objectKey = savedKey });
        })
        .WithName("LocalFileUpload")
        .WithSummary("อัปโหลดไฟล์ตรง (Dev only — สำหรับ Local File Storage)")
        .DisableAntiforgery();

        // GET /api/documents/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            Tms.Documents.Domain.Interfaces.IDocumentRepository repo,
            CancellationToken ct) =>
        {
            var doc = await repo.GetByIdAsync(id, ct);
            if (doc is null || doc.IsDeleted) return Results.NotFound();
            return Results.Ok(new
            {
                doc.Id, doc.FileName, doc.ContentType, doc.FileSizeBytes,
                Category = doc.Category.ToString(), doc.OwnerId, doc.OwnerType,
                doc.UploadedAt, doc.ExpiresAt
            });
        })
        .WithName("GetDocument")
        .WithSummary("ดู Document Metadata");

        // GET /api/documents/{id}/download-url
        group.MapGet("/{id:guid}/download-url", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDownloadUrlQuery(id, Guid.Empty), ct);
            return Results.Ok(result);
        })
        .WithName("GetDocumentDownloadUrl")
        .WithSummary("ขอ Temporary Download URL");

        // GET /api/documents/owners/{ownerType}/{ownerId}
        group.MapGet("/owners/{ownerType}/{ownerId:guid}", async (
            string ownerType,
            Guid ownerId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDocumentsByOwnerQuery(ownerId, ownerType, Guid.Empty), ct);
            return Results.Ok(result);
        })
        .WithName("GetDocumentsByOwner")
        .WithSummary("เอกสารทั้งหมดของ Entity นั้น");

        return app;
    }
}

// Request Records
public sealed record CreateUploadSessionRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Category,
    Guid OwnerId,
    string OwnerType);
