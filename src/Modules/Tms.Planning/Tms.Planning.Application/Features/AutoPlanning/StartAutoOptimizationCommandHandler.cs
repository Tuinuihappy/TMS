using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tms.Planning.Application.Common.Interfaces;
using Tms.Planning.Domain.Entities;
using Tms.Planning.Domain.Enums;
using Tms.SharedKernel.Application;

namespace Tms.Planning.Application.Features.AutoPlanning;

public sealed class StartAutoOptimizationCommandHandler(
    IPlanningDbContext dbContext,
    IServiceProvider serviceProvider,
    ILogger<StartAutoOptimizationCommandHandler> logger) : IRequestHandler<StartAutoOptimizationCommand>
{
    // สมมติว่ารถใหญ่สุดรับได้ 5000 kg แบบเหมา
    private const decimal MaxTruckWeightCapacity = 5000m; 

    public async Task Handle(StartAutoOptimizationCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Entering Phase 2: Async Pipeline for session {SessionId}", request.SessionId);

        var orders = await dbContext.PlanningOrders
            .Where(o => request.PlanningOrderIds.Contains(o.Id) && o.Status == PlanningOrderStatus.InPlanning && o.CurrentProcessingSessionId == request.SessionId)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0) return;

        var validOrders = new List<PlanningOrder>();
        var invalidOrders = new List<PlanningOrder>();

        // --- 1. Pre-check Validation ---
        foreach(var order in orders)
        {
            // ตรวจสอบพิกัด Latitude, Longitude ว่าถูกต้องไหม
            if (order.PickupLatitude == 0 || order.PickupLongitude == 0 || 
                order.DropoffLatitude == 0 || order.DropoffLongitude == 0)
            {
                logger.LogWarning("Validation failed for Order {OrderNumber}: Missing coordinates.", order.OrderNumber);
                invalidOrders.Add(order);
                continue;
            }
            
            // ตรวจสอบขั้นต่ำ TimeWindow เป็นไปได้จริงหรือไม่
            if (order.ReadyTime.HasValue && order.DueTime.HasValue && order.ReadyTime > order.DueTime)
            {
                logger.LogWarning("Validation failed for Order {OrderNumber}: ReadyTime is after DueTime.", order.OrderNumber);
                invalidOrders.Add(order);
                continue;
            }

            validOrders.Add(order);
        }

        // คืนสถานะ Invalid กลับไปเป็น Unplanned หรือ Discard (ในที่นี้ขอกลับเป็น Unplanned แล้วเอา session ออก เผื่อเติมข้อมูล)
        foreach(var invalid in invalidOrders)
        {
            invalid.RevertToUnplanned();
        }

        // --- 2. Split Order ---
        var splittedFakeRequests = new List<PlanningOrder>(); // ในระบบจริงต้องยิง Event ไปให้ Orders Module ทำ Split หรือสร้าง Sub-orders
        foreach (var order in validOrders)
        {
            if (order.TotalWeight > MaxTruckWeightCapacity)
            {
                logger.LogInformation("Order {OrderNumber} exceeds max capacity ({Weight} > {Max}). Triggering Split.", order.OrderNumber, order.TotalWeight, MaxTruckWeightCapacity);
                // สาธิต Logic: แบ่งเป็น 2 รายการสมมติ (จริง ๆ ต้องแจ้ง Order Module)
                // For now, we just pass the original or tell logic it needs multiple trips.
            }
        }

        // --- 3. Consolidation ---
        // (ตัวอย่าง) ถ้าย่านเดียวกัน (lat/long ใกล้กันมาก) ส่งด้วยกันได้ 
        // ปกติอัลกอริทึม VRP จะทำให้อยู่แล้ว แต่ถ้าระบบอยากทำแบบบังคับ (Hard Consolidate) อาจจะผูก ID ไว้ด้วยกัน

        // --- 4. เตรียมข้อมูลส่งเข้า Optimization Engine ---
        if (validOrders.Count > 0)
        {
            // Serialize Parameters constraints 
            var parametersObj = new 
            {
                 SessionId = request.SessionId,
                 OrderIds = validOrders.Select(o => o.OrderId).ToList(),
                 Strategy = "MinimizeDistance"
            };

            var optReq = OptimizationRequest.Create(
                request.TenantId,
                JsonSerializer.Serialize(parametersObj));

            dbContext.OptimizationRequests.Add(optReq);
            
            logger.LogInformation("Created OptimizationRequest {OptId} for {Count} valid orders. Ready for VRP.", optReq.Id, validOrders.Count);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        
        // --- 5. Dispatch to VRP Engine (inline await — เราอยู่ใน Background Job อยู่แล้ว) ---
        if (validOrders.Count > 0)
        {
            var optId = dbContext.OptimizationRequests.Local.Last().Id;
            var processCommand = new ProcessOptimizationRequestCommand(optId);
            
            // ใช้ new scope เพื่อให้ได้ DbContext ใหม่ (ไม่ปนกับ scope เดิม)
            using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(serviceProvider);
            var mediator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMediator>(scope.ServiceProvider);
            await mediator.Send(processCommand, cancellationToken);
        }
    }
}
