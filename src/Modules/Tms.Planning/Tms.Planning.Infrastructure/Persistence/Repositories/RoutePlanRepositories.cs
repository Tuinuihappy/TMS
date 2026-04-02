using Microsoft.EntityFrameworkCore;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Interfaces;
using Tms.Planning.Infrastructure.Persistence;

namespace Tms.Planning.Infrastructure.Persistence.Repositories;

public sealed class RoutePlanRepository(PlanningDbContext context) : IRoutePlanRepository
{
    public async Task<RoutePlan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.RoutePlans
            .Include(p => p.Stops)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<RoutePlan>> GetByDateAsync(
        DateOnly date, Guid? tenantId, CancellationToken ct = default)
    {
        var query = context.RoutePlans
            .Include(p => p.Stops)
            .Where(p => p.PlannedDate == date);
        if (tenantId.HasValue)
            query = query.Where(p => p.TenantId == tenantId.Value);
        return await query.OrderBy(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(RoutePlan plan, CancellationToken ct = default)
    {
        context.RoutePlans.Add(plan);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<RoutePlan> plans, CancellationToken ct = default)
    {
        context.RoutePlans.AddRange(plans);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RoutePlan plan, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);

    public async Task<string> GeneratePlanNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"PLN-{today:yyyyMMdd}";
        var count = await context.RoutePlans
            .CountAsync(p => p.PlanNumber.StartsWith(prefix), ct);
        return $"{prefix}-{(count + 1):D3}";
    }
}

public sealed class OptimizationRequestRepository(PlanningDbContext context) : IOptimizationRequestRepository
{
    public async Task<OptimizationRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.OptimizationRequests
            .Include(r => r.Plans)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(OptimizationRequest request, CancellationToken ct = default)
    {
        context.OptimizationRequests.Add(request);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(OptimizationRequest request, CancellationToken ct = default) =>
        await context.SaveChangesAsync(ct);
}
