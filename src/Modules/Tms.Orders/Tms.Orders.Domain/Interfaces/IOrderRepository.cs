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
}
