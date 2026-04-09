using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tms.Planning.Application.Common.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Features.RoutePlans;

public sealed class LockRoutePlanCommandHandler(
    IPlanningDbContext dbContext,
    IIntegrationEventPublisher eventPublisher,
    ILogger<LockRoutePlanCommandHandler> logger) : IRequestHandler<LockRoutePlanCommand>
{
    public async Task Handle(LockRoutePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await dbContext.RoutePlans
            .Include(p => p.Stops)
            .FirstOrDefaultAsync(p => p.Id == request.RoutePlanId, cancellationToken);

        if (plan == null)
            throw new NotFoundException("RoutePlan", request.RoutePlanId);

        // 1. เปลี่ยนสถานะเป็น Locked (มีโดเมนรูลตรวจสอบใน Entity เรียบร้อย)
        plan.Lock();
        
        logger.LogInformation("RoutePlan {PlanId} successfully locked. Preparing Event...", plan.Id);

        // 2. สร้าง Snapshot ของ Stop
        var stopSnapshots = plan.Stops.Select(s => new RoutePlanStopSnapshot(
            Sequence: s.Sequence,
            OrderId: s.OrderId,
            StopType: s.StopType,
            Latitude: s.Latitude,
            Longitude: s.Longitude,
            EstimatedArrivalTime: s.EstimatedArrivalTime
        )).ToList();

        // 3. ยิง Integration Event (เข้า Outbox แล้วเข้า RabbitMQ สู่ Execution Module ต่อไป)
        var integrationEvent = new RoutePlanLockedIntegrationEvent(
            RoutePlanId: plan.Id,
            VehicleTypeId: plan.VehicleTypeId,
            PlannedDate: plan.PlannedDate,
            TenantId: plan.TenantId,
            Stops: stopSnapshots
        );

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        // 4. SaveChanges จะทำการเก็บ RoutePlan state update พร้อมกับบันทึก Outbox Message ใน transaction เดียวกัน
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully published RoutePlanLockedIntegrationEvent for RoutePlan {PlanId}", plan.Id);
    }
}
