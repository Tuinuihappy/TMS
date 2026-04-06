using Microsoft.EntityFrameworkCore;
using Tms.Documents.Domain.Aggregates;
using Tms.Documents.Domain.Entities;
using Tms.Documents.Domain.Interfaces;

namespace Tms.Documents.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(DocumentsDbContext db) : IDocumentRepository
{
    public Task<StoredDocument?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.StoredDocuments.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<StoredDocument>> GetByOwnerAsync(Guid ownerId, string ownerType, CancellationToken ct) =>
        db.StoredDocuments
            .Where(x => x.OwnerId == ownerId && x.OwnerType == ownerType && !x.IsDeleted)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync(ct);

    public async Task AddAsync(StoredDocument document, CancellationToken ct)
    {
        db.StoredDocuments.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(StoredDocument document, CancellationToken ct)
    {
        db.StoredDocuments.Update(document);
        await db.SaveChangesAsync(ct);
    }

    public Task<UploadSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct) =>
        db.UploadSessions.FirstOrDefaultAsync(x => x.Id == sessionId, ct);

    public async Task AddSessionAsync(UploadSession session, CancellationToken ct)
    {
        db.UploadSessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSessionAsync(UploadSession session, CancellationToken ct)
    {
        db.UploadSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
