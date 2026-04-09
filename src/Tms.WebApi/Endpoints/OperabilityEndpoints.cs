using Microsoft.EntityFrameworkCore;
using Tms.Execution.Infrastructure.Persistence;
using Tms.Orders.Infrastructure.Persistence;
using Tms.Planning.Infrastructure.Persistence;
using Tms.SharedKernel.Infrastructure.Outbox;

namespace Tms.WebApi.Endpoints;

public static class OperabilityEndpoints
{
    public static IEndpointRouteBuilder MapOperabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/operability")
            .WithTags("Operability");

        // ── Dead Letter Queue ─────────────────────────────────────────────
        group.MapGet("/dlq", GetDeadLetterMessages)
            .WithName("GetDeadLetterMessages")
            .WithSummary("List all dead-letter outbox messages.");

        group.MapPost("/dlq/{id:guid}/retry", RetryDeadLetterMessage)
            .WithName("RetryDeadLetterMessage")
            .WithSummary("Reset a dead-letter outbox message for retry.");

        // ── Reconciliation Report ─────────────────────────────────────────
        group.MapGet("/reconciliation/report", GetReconciliationReport)
            .WithName("GetReconciliationReport")
            .WithSummary("Returns a snapshot of stale or inconsistent records.");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private static async Task<IResult> GetDeadLetterMessages(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var results = new List<object>();

        var contexts = new (string Module, DbContext Ctx)[]
        {
            ("Orders",    scope.ServiceProvider.GetRequiredService<OrdersDbContext>()),
            ("Planning",  scope.ServiceProvider.GetRequiredService<PlanningDbContext>()),
            ("Execution", scope.ServiceProvider.GetRequiredService<ExecutionDbContext>()),
        };

        foreach (var (module, ctx) in contexts)
        {
            var dlq = await ctx.Set<OutboxMessage>()
                .Where(m => m.IsDeadLetter && m.ProcessedOn == null)
                .OrderByDescending(m => m.OccurredOn)
                .Take(100)
                .Select(m => new
                {
                    m.Id, Module = module, m.Type, m.RetryCount,
                    m.OccurredOn, m.Error
                })
                .ToListAsync();

            results.AddRange(dlq);
        }

        return Results.Ok(results.OrderByDescending(x => ((dynamic)x).OccurredOn));
    }

    private static async Task<IResult> RetryDeadLetterMessage(Guid id, IServiceProvider sp)
    {
        using var scope = sp.CreateScope();

        var contexts = new DbContext[]
        {
            scope.ServiceProvider.GetRequiredService<OrdersDbContext>(),
            scope.ServiceProvider.GetRequiredService<PlanningDbContext>(),
            scope.ServiceProvider.GetRequiredService<ExecutionDbContext>(),
        };

        foreach (var ctx in contexts)
        {
            var msg = await ctx.Set<OutboxMessage>().FindAsync(id);
            if (msg is null) continue;

            // Reset for retry
            msg.IsDeadLetter = false;
            msg.RetryCount   = 0;
            msg.NextRetryAt  = null;
            msg.Error        = null;
            await ctx.SaveChangesAsync();

            return Results.NoContent();
        }

        return Results.NotFound(new { Error = $"Dead-letter message {id} not found in any module." });
    }

    private static async Task<IResult> GetReconciliationReport(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var ordersCtx    = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var planningCtx  = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();
        var executionCtx = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();

        var now = DateTime.UtcNow;

        var stalePlannedOrders = await ordersCtx
            .Set<Orders.Domain.Entities.TransportOrder>()
            .CountAsync(o => o.Status == Orders.Domain.Enums.OrderStatus.Planned
                          && o.UpdatedAt < now.AddHours(-24));

        var staleInProgressTrips = await planningCtx.Trips
            .CountAsync(t => (t.Status == Planning.Domain.Entities.TripStatus.InProgress
                           || t.Status == Planning.Domain.Entities.TripStatus.Dispatched)
                          && t.DispatchedAt < now.AddHours(-12));

        var staleShipments = await executionCtx.Shipments
            .CountAsync(s => (s.Status == Execution.Domain.Enums.ShipmentStatus.PickedUp
                           || s.Status == Execution.Domain.Enums.ShipmentStatus.InTransit)
                          && s.PickedUpAt < now.AddHours(-24));

        var totalDlq = 0;
        foreach (var ctx in new DbContext[] { ordersCtx, planningCtx, executionCtx })
            totalDlq += await ctx.Set<OutboxMessage>().CountAsync(m => m.IsDeadLetter && m.ProcessedOn == null);

        return Results.Ok(new
        {
            GeneratedAt            = now,
            StalePlannedOrders     = stalePlannedOrders,
            StaleInProgressTrips   = staleInProgressTrips,
            StalePickedUpShipments = staleShipments,
            DeadLetterMessages     = totalDlq,
            Status = stalePlannedOrders + staleInProgressTrips + staleShipments + totalDlq == 0
                ? "Healthy" : "NeedsAttention"
        });
    }
}
