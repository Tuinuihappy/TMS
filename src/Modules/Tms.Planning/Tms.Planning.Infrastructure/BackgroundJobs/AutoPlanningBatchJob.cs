using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tms.Planning.Domain.Enums;
using Tms.Planning.Infrastructure.Persistence;

namespace Tms.Planning.Infrastructure.BackgroundJobs;

/// <summary>
/// Background Job: แบบ B (Batch) ที่จะตื่นขึ้นมาทุกๆ 5-15 นาที 
/// เพื่อโกย Orders ที่เป็น `Unplanned` ส่งให้ Pipeline ของ VRP
/// </summary>
public class AutoPlanningBatchJob(
    IServiceProvider serviceProvider,
    ILogger<AutoPlanningBatchJob> logger) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AutoPlanningBatchJob started with polling interval {Interval}", _pollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnplannedOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while polling Unplanned orders.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessUnplannedOrdersAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlanningDbContext>();

        // 1. หาออเดอร์ที่ยังไม่ได้แพลน (Unplanned) 
        var unplannedOrders = await dbContext.PlanningOrders
            .Where(o => o.Status == PlanningOrderStatus.Unplanned)
            .OrderBy(o => o.ReadyTime ?? DateTime.MinValue)
            .Take(100) // สมมติว่าดึง Batch ละ 100 ออเดอร์
            .ToListAsync(stoppingToken);

        if (unplannedOrders.Count == 0)
        {
            return; // ไม่มีออเดอร์ให้แพลน
        }

        logger.LogInformation("Found {Count} unplanned orders. Starting auto-planning...", unplannedOrders.Count);

        var sessionId = Guid.NewGuid();

        // 2. State Locking (Optimistic Concurrency)
        foreach (var order in unplannedOrders)
        {
            try
            {
                order.LockForPlanning(sessionId);
            }
            catch(Exception ex)
            {
                logger.LogWarning(ex, "Failed to lock order {OrderNumber}. Skipping.", order.OrderNumber);
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
        
        // --- ณ จุดนี้ Orders ทั้งหมดติดสถานะ InPlanning (กันการแย่งทำงาน) ---
        
        // 3. โยน List ของถูก Lock เข้า Async Pipeline (MediatR Command)
        // เพื่อให้ Worker ไปทำการ Validation, Split, Consolidate และเริ่ม Optimization ต่อไป
        var orderIds = unplannedOrders.Select(o => o.Id).ToList();
        var command = new Tms.Planning.Application.Features.AutoPlanning.StartAutoOptimizationCommand(sessionId, orderIds, unplannedOrders.First().TenantId);
        
        // เราใช้ IServiceScope ในการ resolve IMediator
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        await mediator.Send(command, stoppingToken);

        logger.LogInformation("Successfully dispatched {Count} orders for session {SessionId} to VRP pipeline.", unplannedOrders.Count, sessionId);
    }
}
