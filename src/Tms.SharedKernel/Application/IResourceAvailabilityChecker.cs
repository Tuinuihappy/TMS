namespace Tms.SharedKernel.Application;

/// <summary>
/// Cross-module validation service for vehicle/driver availability.
/// Defined in SharedKernel so both Planning and Resources modules can reference it
/// without circular dependencies.
/// </summary>
public interface IResourceAvailabilityChecker
{
    Task<bool> IsVehicleAvailableAsync(Guid vehicleId, CancellationToken ct = default);
    Task<bool> IsDriverAvailableAsync(Guid driverId, CancellationToken ct = default);
}
