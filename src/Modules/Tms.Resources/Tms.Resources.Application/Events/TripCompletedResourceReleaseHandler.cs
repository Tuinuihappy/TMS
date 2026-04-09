using MediatR;
using Tms.Resources.Domain.Entities;
using Tms.Resources.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Resources.Application.Events;

/// <summary>
/// เมื่อ Trip Complete → auto-release Vehicle และ Driver กลับเป็น Available
/// และบันทึก HOS (Hours of Service) สำหรับ Driver จาก estimated duration
/// </summary>
public sealed class TripCompletedResourceReleaseHandler(
    IVehicleRepository vehicleRepo,
    IDriverRepository driverRepo)
    : INotificationHandler<TripCompletedIntegrationEvent>
{
    public async Task Handle(TripCompletedIntegrationEvent ev, CancellationToken ct)
    {
        // ── Release Vehicle ────────────────────────────────────────────────
        if (ev.VehicleId.HasValue)
        {
            var vehicle = await vehicleRepo.GetByIdAsync(ev.VehicleId.Value, ct);
            if (vehicle is not null && vehicle.Status == VehicleStatus.InUse)
            {
                vehicle.ChangeStatus(VehicleStatus.Available);
                await vehicleRepo.UpdateAsync(vehicle, ct);
            }
        }

        // ── Release Driver + Record HOS ───────────────────────────────────
        if (ev.DriverId.HasValue)
        {
            var driver = await driverRepo.GetByIdAsync(ev.DriverId.Value, ct);
            if (driver is not null && driver.Status == DriverStatus.OnDuty)
            {
                // Record estimated HOS — 0 driving hours (actual HOS tracked via trip duration)
                // In production this would come from actual telematics data
                driver.RecordHOS(drivingHours: 0, restingHours: 0, tripId: ev.TripId);
                driver.ChangeStatus(DriverStatus.Available);
                await driverRepo.UpdateAsync(driver, ct);
            }
        }
    }
}
