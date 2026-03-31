using Microsoft.EntityFrameworkCore;
using Tms.Orders.Domain.Entities;
using Tms.Orders.Domain.Interfaces;
using Tms.Orders.Infrastructure.Persistence;
using Tms.SharedKernel.Exceptions;

namespace Tms.Orders.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(OrdersDbContext context) : IOrderRepository
{
    public async Task<TransportOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.TransportOrders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<TransportOrder?> GetByOrderNumberAsync(
        string orderNumber, CancellationToken cancellationToken = default) =>
        await context.TransportOrders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public async Task<(IReadOnlyList<TransportOrder> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.TransportOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status.ToString() == status);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(TransportOrder entity, CancellationToken cancellationToken = default)
    {
        await context.TransportOrders.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TransportOrder entity, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TransportOrder entity, CancellationToken cancellationToken = default)
    {
        context.TransportOrders.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"ORD-{today:yyyyMMdd}";
        var count = await context.TransportOrders
            .CountAsync(o => o.OrderNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}-{(count + 1):D4}";
    }
}
