using Tms.Orders.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.Interfaces;

public interface IOrderRepository : IRepository<TransportOrder>
{
    Task<TransportOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TransportOrder> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);

    // ── Split Order ───────────────────────────────────────────────────────────
    /// <summary>บันทึก child orders หลายใบใน transaction เดียวกัน</summary>
    Task AddRangeAsync(IEnumerable<TransportOrder> orders, CancellationToken cancellationToken = default);
    /// <summary>ดึง child orders ทั้งหมดของ parent order</summary>
    Task<IReadOnlyList<TransportOrder>> GetChildOrdersAsync(Guid parentOrderId, CancellationToken cancellationToken = default);
    /// <summary>ดึง orders หลายรายการตาม IDs</summary>
    Task<IReadOnlyList<TransportOrder>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
