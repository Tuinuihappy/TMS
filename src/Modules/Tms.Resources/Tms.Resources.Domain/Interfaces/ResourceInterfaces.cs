using Tms.Resources.Domain.Entities;
using Tms.SharedKernel.Domain;

namespace Tms.Resources.Domain.Interfaces;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<Vehicle?> GetByPlateNumberAsync(string plateNumber, CancellationToken ct = default);
    Task<bool> ExistsByPlateNumberAsync(string plateNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null, Guid? vehicleTypeId = null, Guid? tenantId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> GetAvailableAsync(
        Guid tenantId, decimal? minPayloadKg = null, bool includeDetails = false,
        CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> GetExpiryAlertsAsync(Guid tenantId, int withinDays = 30, CancellationToken ct = default);
}

public interface IDriverRepository : IRepository<Driver>
{
    Task<Driver?> GetByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default);
    Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default);
    Task<(IReadOnlyList<Driver> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null, string? licenseType = null, Guid? tenantId = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Driver>> GetAvailableAsync(
        Guid tenantId, string? requiredLicenseType = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Driver>> GetExpiryAlertsAsync(Guid tenantId, int withinDays = 30, CancellationToken ct = default);
}

public interface IVehicleTypeRepository : IRepository<VehicleType>
{
    Task<IReadOnlyList<VehicleType>> GetAllByTenantAsync(Guid tenantId, CancellationToken ct = default);
}
