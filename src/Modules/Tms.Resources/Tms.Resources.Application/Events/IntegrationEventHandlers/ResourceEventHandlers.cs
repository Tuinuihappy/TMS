using MediatR;
using Tms.Resources.Domain.Entities;
using Tms.Resources.Domain.Interfaces;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Resources.Application.Events.IntegrationEventHandlers;

public sealed class TripDispatchedResourceHandler(
    IVehicleRepository vehicleRepo,
    IDriverRepository driverRepo)
    : INotificationHandler<TripDispatchedIntegrationEvent>
{
    public async Task Handle(TripDispatchedIntegrationEvent notification, CancellationToken ct)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(notification.VehicleId, ct);
        if (vehicle is not null && vehicle.Status == VehicleStatus.Available)
        {
            vehicle.ChangeStatus(VehicleStatus.InUse);
            await vehicleRepo.UpdateAsync(vehicle, ct);
        }

        var driver = await driverRepo.GetByIdAsync(notification.DriverId, ct);
        if (driver is not null && driver.Status == DriverStatus.Available)
        {
            driver.ChangeStatus(DriverStatus.OnDuty);
            await driverRepo.UpdateAsync(driver, ct);
        }
    }
}

public sealed class TripCompletedResourceHandler(
    IVehicleRepository vehicleRepo,
    IDriverRepository driverRepo)
    : INotificationHandler<TripCompletedIntegrationEvent>
{
    public async Task Handle(TripCompletedIntegrationEvent notification, CancellationToken ct)
    {
        if (notification.VehicleId.HasValue)
        {
            var vehicle = await vehicleRepo.GetByIdAsync(notification.VehicleId.Value, ct);
            if (vehicle is not null && vehicle.Status == VehicleStatus.InUse)
            {
                vehicle.ChangeStatus(VehicleStatus.Available);
                await vehicleRepo.UpdateAsync(vehicle, ct);
            }
        }

        if (notification.DriverId.HasValue)
        {
            var driver = await driverRepo.GetByIdAsync(notification.DriverId.Value, ct);
            if (driver is not null && driver.Status == DriverStatus.OnDuty)
            {
                driver.ChangeStatus(DriverStatus.Available);
                await driverRepo.UpdateAsync(driver, ct);
            }
        }
    }
}

public sealed class TripCancelledResourceHandler(
    IVehicleRepository vehicleRepo,
    IDriverRepository driverRepo)
    : INotificationHandler<TripCancelledIntegrationEvent>
{
    public async Task Handle(TripCancelledIntegrationEvent notification, CancellationToken ct)
    {
        if (notification.VehicleId.HasValue)
        {
            var vehicle = await vehicleRepo.GetByIdAsync(notification.VehicleId.Value, ct);
            if (vehicle is not null && vehicle.Status is VehicleStatus.InUse or VehicleStatus.Assigned)
            {
                vehicle.ChangeStatus(VehicleStatus.Available);
                await vehicleRepo.UpdateAsync(vehicle, ct);
            }
        }

        if (notification.DriverId.HasValue)
        {
            var driver = await driverRepo.GetByIdAsync(notification.DriverId.Value, ct);
            if (driver is not null && driver.Status == DriverStatus.OnDuty)
            {
                driver.ChangeStatus(DriverStatus.Available);
                await driverRepo.UpdateAsync(driver, ct);
            }
        }
    }
}
