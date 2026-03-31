using Tms.Resources.Domain.Entities;
using Tms.Resources.Domain.Interfaces;
using Tms.SharedKernel.Application;

namespace Tms.Resources.Application.Features;

/// <summary>
/// Implementation of IResourceAvailabilityChecker that lives in the Resources module
/// but is registered globally. Provides cross-module validation for Trip assignment.
/// </summary>
public sealed class ResourceAvailabilityChecker(
    IVehicleRepository vehicleRepo,
    IDriverRepository driverRepo) : IResourceAvailabilityChecker
{
    public async Task<bool> IsVehicleAvailableAsync(Guid vehicleId, CancellationToken ct = default)
    {
        var vehicle = await vehicleRepo.GetByIdAsync(vehicleId, ct);
        if (vehicle is null) return false;

        return vehicle.Status == VehicleStatus.Available && vehicle.IsRegistrationValid();
    }

    public async Task<bool> IsDriverAvailableAsync(Guid driverId, CancellationToken ct = default)
    {
        var driver = await driverRepo.GetByIdAsync(driverId, ct);
        if (driver is null) return false;

        return driver.Status == DriverStatus.Available && !driver.License.IsExpired();
    }
}
