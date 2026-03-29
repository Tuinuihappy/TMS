using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Planning.Domain.Interfaces;

public interface ITripRepository : IRepository<Trip>
{
    Task<Trip?> GetByTripNumberAsync(string tripNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Trip> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        DateOnly? plannedDate = null,
        Guid? tenantId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Trip>> GetByDateAsync(DateOnly date, Guid tenantId, CancellationToken ct = default);
    Task<string> GenerateTripNumberAsync(CancellationToken ct = default);
}
