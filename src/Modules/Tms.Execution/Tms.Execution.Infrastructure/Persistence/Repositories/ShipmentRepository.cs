using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Entities;
using Tms.Execution.Domain.Interfaces;
using Tms.Execution.Infrastructure.Persistence;
using Tms.SharedKernel.Exceptions;

namespace Tms.Execution.Infrastructure.Persistence.Repositories;

public sealed class ShipmentRepository(ExecutionDbContext context) : IShipmentRepository
{
    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Shipments
            .Include(s => s.POD)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Shipment?> GetByShipmentNumberAsync(string shipmentNumber, CancellationToken ct = default) =>
        await context.Shipments
            .Include(s => s.POD)
            .FirstOrDefaultAsync(s => s.ShipmentNumber == shipmentNumber, ct);

    public async Task<(IReadOnlyList<Shipment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        Guid? tripId = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = context.Shipments.Include(s => s.POD).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status.ToString() == status);
        if (tripId.HasValue)
            query = query.Where(s => s.TripId == tripId.Value);
        if (tenantId.HasValue)
            query = query.Where(s => s.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Shipment>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default) =>
        await context.Shipments
            .Where(s => s.TripId == tripId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Shipment>> GetByTenantPendingAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Shipments
            .Where(s => s.TenantId == tenantId
                     && (s.Status == Execution.Domain.Enums.ShipmentStatus.PickedUp
                      || s.Status == Execution.Domain.Enums.ShipmentStatus.InTransit))
            .ToListAsync(ct);

    public async Task AddAsync(Shipment entity, CancellationToken ct = default)
    {
        await context.Shipments.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Shipment entity, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Shipment entity, CancellationToken ct = default)
    {
        context.Shipments.Remove(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<string> GenerateShipmentNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"SHP-{today:yyyyMMdd}";
        var count = await context.Shipments
            .CountAsync(s => s.ShipmentNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D4}";
    }

    public async Task AddPodRecordAsync(PODRecord pod, CancellationToken ct = default)
    {
        await context.PODRecords.AddAsync(pod, ct);
        await context.SaveChangesAsync(ct);
    }
}
