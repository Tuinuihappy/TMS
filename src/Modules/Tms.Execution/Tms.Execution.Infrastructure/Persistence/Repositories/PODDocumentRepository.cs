using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.Execution.Infrastructure.Persistence;

namespace Tms.Execution.Infrastructure.Persistence.Repositories;

public sealed class PODDocumentRepository(ExecutionDbContext context) : IPODDocumentRepository
{
    public async Task<PODDocument?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken ct = default) =>
        await context.PODDocuments
            .Include(p => p.Verifications)
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId, ct);

    public async Task AddAsync(PODDocument document, CancellationToken ct = default)
    {
        context.PODDocuments.Add(document);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PODDocument document, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}
