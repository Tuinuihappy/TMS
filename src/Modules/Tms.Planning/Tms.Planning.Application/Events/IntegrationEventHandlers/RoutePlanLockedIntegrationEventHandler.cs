using MediatR;
using Microsoft.Extensions.Logging;
using Tms.Planning.Application.Common.Interfaces;
using Tms.Planning.Domain.Entities;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Planning.Application.Events.IntegrationEventHandlers;

public sealed class RoutePlanLockedIntegrationEventHandler(
    IPlanningDbContext dbContext,
    ILogger<RoutePlanLockedIntegrationEventHandler> logger) : INotificationHandler<RoutePlanLockedIntegrationEvent>
{
    public async Task Handle(RoutePlanLockedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating Trip from locked RoutePlan {RoutePlanId}", notification.RoutePlanId);

        // 1. ตรวจสอบว่า Trip นี้เคยถูกสร้างแล้วหรือยังเพื่อความชัวร์ (Idempotency)
        // สร้างรหัส Trip โดยอิงจาก RoutePlanId หรือให้เป็นรหัสใหม่
        var tripNumber = $"TRP-{DateTime.UtcNow:yyyyMMdd}-{notification.RoutePlanId.ToString()[..4].ToUpper()}";
        
        var existingTrip = dbContext.Trips.Any(t => t.TripNumber == tripNumber);
        if (existingTrip)
        {
            logger.LogWarning("Trip {TripNumber} already exists. Skipping Trip creation.", tripNumber);
            return;
        }

        // 2. สร้าง Trip Aggregate
        var newTrip = Trip.Create(
            tripNumber: tripNumber,
            plannedDate: notification.PlannedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            tenantId: notification.TenantId
            // TODO: สามารถดึง TotalWeight, TotalVolume จาก RoutePlan ส่งมากับ Event ได้
        );

        // 3. ปรับค่า Stop จาก Snapshot ของ RoutePlan ให้กลายเป็น Stop ของ Trip
        foreach (var stop in notification.Stops)
        {
            var stopType = stop.StopType == "Pickup" ? StopType.Pickup : StopType.Dropoff;
            newTrip.AddStop(
                sequence: stop.Sequence,
                orderId: stop.OrderId,
                type: stopType,
                lat: stop.Latitude,
                lng: stop.Longitude,
                windowFrom: stop.EstimatedArrivalTime,
                windowTo: stop.EstimatedArrivalTime?.AddHours(1) // สมมติให้ ETA = WindowFrom, WindowTo = ETA+1
            );
        }

        // 4. บันทึก Trip ลง Database
        dbContext.Trips.Add(newTrip);

        // 5. หาก RoutePlan มีการระบุ VehicleTypeId มาด้วย (ในกรณีที่ Auto Assign ได้) ก็อาจจะโยงต่อไปหา VehicleId ได้
        // เพื่อตั้งเป็น Assigned ทันที 

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully created Trip {TripNumber} with {Count} stops.", newTrip.TripNumber, newTrip.Stops.Count);
    }
}
