using System.Data;
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
        {
            if (Enum.TryParse<Domain.Enums.OrderStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);
            else
                return ([], 0);
        }

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefix = $"ORD-{today:yyyyMMdd}";

        var conn = context.Database.GetDbConnection();
        var needsOpen = conn.State != ConnectionState.Open;
        if (needsOpen) await conn.OpenAsync(cancellationToken);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ord."OrderDailyCounters" ("Date", "LastSequence")
                VALUES (@date, 1)
                ON CONFLICT ("Date") DO UPDATE
                SET "LastSequence" = ord."OrderDailyCounters"."LastSequence" + 1
                RETURNING "LastSequence"
                """;
            var p = cmd.CreateParameter();
            p.ParameterName = "date";
            p.Value = today;
            cmd.Parameters.Add(p);

            var seq = (int)(await cmd.ExecuteScalarAsync(cancellationToken))!;
            return $"{prefix}-{seq:D4}";
        }
        finally
        {
            if (needsOpen) await conn.CloseAsync();
        }
    }

    public async Task<IReadOnlyList<TransportOrder>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await context.TransportOrders
            .Where(o => idList.Contains(o.Id))
            .ToListAsync(cancellationToken);
    }
}
