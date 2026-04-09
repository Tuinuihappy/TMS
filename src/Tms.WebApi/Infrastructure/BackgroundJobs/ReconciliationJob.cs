using Microsoft.EntityFrameworkCore;
using Tms.Execution.Domain.Enums;
using Tms.Execution.Infrastructure.Persistence;
using Tms.Orders.Infrastructure.Persistence;
using Tms.Planning.Infrastructure.Persistence;
using Tms.SharedKernel.Infrastructure.Outbox;

namespace Tms.WebApi.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs every hour to detect stale Orders/Trips/Shipments and log alerts.
/// Does NOT auto-modify state — only raises structured log warnings that can be routed to alerting.
/// </summary>
public sealed class ReconciliationJob(
    IServiceProvider serviceProvider,
    ILogger<ReconciliationJob> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval          = TimeSpan.FromHours(1);
    private static readonly TimeSpan OrderPlannedStale    = TimeSpan.FromHours(24);
    private static readonly TimeSpan TripInProgressStale  = TimeSpan.FromHours(12);
    private static readonly TimeSpan ShipmentPickedUpStale = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay: give the app time to fully start before first run
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunReconciliationAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Error in ReconciliationJob."); }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunReconciliationAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        await CheckStalePlannedOrdersAsync(sp, ct);
        await CheckStaleTripsAsync(sp, ct);
        await CheckStaleShipmentsAsync(sp, ct);
        await CheckDeadLetterOutboxAsync(sp, ct);
    }

    // ── Stale Orders ────────────────────────────────────────────────────
    private async Task CheckStalePlannedOrdersAsync(IServiceProvider sp, CancellationToken ct)
    {
        var ctx = sp.GetRequiredService<OrdersDbContext>();
        var threshold = DateTime.UtcNow - OrderPlannedStale;

        var staleOrders = await ctx.Set<Tms.Orders.Domain.Entities.TransportOrder>()
            .Where(o => o.Status == Orders.Domain.Enums.OrderStatus.Planned
                     && o.UpdatedAt < threshold)
            .Select(o => new { o.Id, o.OrderNumber, o.UpdatedAt })
            .ToListAsync(ct);

        foreach (var o in staleOrders)
            logger.LogWarning(
                "[Reconciliation] Stale Planned Order {OrderNumber} ({Id}) — no dispatch in {Hours}h",
                o.OrderNumber, o.Id, OrderPlannedStale.TotalHours);
    }

    // ── Stale Trips ──────────────────────────────────────────────────────
    private async Task CheckStaleTripsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var ctx = sp.GetRequiredService<PlanningDbContext>();
        var threshold = DateTime.UtcNow - TripInProgressStale;

        var staleTrips = await ctx.Trips
            .Where(t => (t.Status == Planning.Domain.Entities.TripStatus.InProgress
                       || t.Status == Planning.Domain.Entities.TripStatus.Dispatched)
                     && t.DispatchedAt < threshold)
            .Select(t => new { t.Id, t.TripNumber, t.DispatchedAt })
            .ToListAsync(ct);

        foreach (var t in staleTrips)
            logger.LogWarning(
                "[Reconciliation] Stale Trip {TripNumber} ({Id}) — InProgress for >{Hours}h",
                t.TripNumber, t.Id, TripInProgressStale.TotalHours);
    }

    // ── Stale Shipments ──────────────────────────────────────────────────
    private async Task CheckStaleShipmentsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var ctx = sp.GetRequiredService<ExecutionDbContext>();
        var threshold = DateTime.UtcNow - ShipmentPickedUpStale;

        var staleShipments = await ctx.Shipments
            .Where(s => (s.Status == ShipmentStatus.PickedUp || s.Status == ShipmentStatus.InTransit)
                     && s.PickedUpAt < threshold)
            .Select(s => new { s.Id, s.ShipmentNumber, s.PickedUpAt })
            .ToListAsync(ct);

        foreach (var s in staleShipments)
            logger.LogWarning(
                "[Reconciliation] Stale Shipment {ShipmentNumber} ({Id}) — PickedUp for >{Hours}h",
                s.ShipmentNumber, s.Id, ShipmentPickedUpStale.TotalHours);
    }

    // ── Dead Letter Outbox ───────────────────────────────────────────────
    private async Task CheckDeadLetterOutboxAsync(IServiceProvider sp, CancellationToken ct)
    {
        var contexts = new (string Module, DbContext Ctx)[]
        {
            ("Orders",    sp.GetRequiredService<OrdersDbContext>()),
            ("Planning",  sp.GetRequiredService<PlanningDbContext>()),
            ("Execution", sp.GetRequiredService<ExecutionDbContext>()),
        };

        foreach (var (module, ctx) in contexts)
        {
            var dlqCount = await ctx.Set<OutboxMessage>()
                .CountAsync(m => m.IsDeadLetter && m.ProcessedOn == null, ct);

            if (dlqCount > 0)
                logger.LogError(
                    "[Reconciliation] Module {Module} has {Count} dead-letter outbox message(s)! Manual retry required.",
                    module, dlqCount);
        }
    }
}
