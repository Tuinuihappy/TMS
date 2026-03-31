using Tms.Execution.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Execution.Domain.Interfaces;

public interface IShipmentRepository : IRepository<Shipment>
{
    Task<Shipment?> GetByShipmentNumberAsync(string shipmentNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Shipment> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        Guid? tripId = null,
        Guid? tenantId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> GetByTripIdAsync(Guid tripId, CancellationToken ct = default);
    Task<string> GenerateShipmentNumberAsync(CancellationToken ct = default);
    Task AddPodRecordAsync(PODRecord pod, CancellationToken ct = default);
}
