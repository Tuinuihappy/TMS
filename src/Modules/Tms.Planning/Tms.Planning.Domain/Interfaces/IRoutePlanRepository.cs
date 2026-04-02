using Tms.Planning.Domain.Entities;

namespace Tms.Planning.Domain.Interfaces;

public interface IRoutePlanRepository
{
    Task<RoutePlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<RoutePlan>> GetByDateAsync(DateOnly date, Guid? tenantId, CancellationToken ct = default);
    Task AddAsync(RoutePlan plan, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<RoutePlan> plans, CancellationToken ct = default);
    Task UpdateAsync(RoutePlan plan, CancellationToken ct = default);
    Task<string> GeneratePlanNumberAsync(CancellationToken ct = default);
}

public interface IOptimizationRequestRepository
{
    Task<OptimizationRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(OptimizationRequest request, CancellationToken ct = default);
    Task UpdateAsync(OptimizationRequest request, CancellationToken ct = default);
}
