using Microsoft.EntityFrameworkCore;
using Tms.Resources.Domain.Entities;
using Tms.Resources.Domain.Interfaces;
using Tms.Resources.Infrastructure.Persistence;

namespace Tms.Resources.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(ResourcesDbContext context) : IVehicleRepository
{
    public async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<Vehicle?> GetByPlateNumberAsync(string plateNumber, CancellationToken ct = default) =>
        await context.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == plateNumber, ct);

    public async Task<bool> ExistsByPlateNumberAsync(string plateNumber, CancellationToken ct = default) =>
        await context.Vehicles.AnyAsync(v => v.PlateNumber == plateNumber, ct);

    public async Task<(IReadOnlyList<Vehicle> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null, Guid? vehicleTypeId = null, Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = context.Vehicles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status.ToString() == status);
        if (vehicleTypeId.HasValue)
            query = query.Where(v => v.VehicleTypeId == vehicleTypeId.Value);
        if (tenantId.HasValue)
            query = query.Where(v => v.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(v => v.PlateNumber)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Vehicle>> GetAvailableAsync(
        Guid tenantId, decimal? minPayloadKg = null, bool includeDetails = false,
        CancellationToken ct = default)
    {
        var query = context.Vehicles
            .Include(v => v.MaintenanceRecords)
            .Where(v => v.TenantId == tenantId
                && v.Status == VehicleStatus.Available
                && (v.RegistrationExpiry == null || v.RegistrationExpiry >= DateTime.UtcNow.Date));

        // Join with VehicleType for payload filter
        if (minPayloadKg.HasValue)
        {
            var validTypeIds = await context.VehicleTypes
                .Where(t => t.MaxPayloadKg >= minPayloadKg.Value)
                .Select(t => t.Id)
                .ToListAsync(ct);
            query = query.Where(v => validTypeIds.Contains(v.VehicleTypeId));
        }

        return await query.OrderBy(v => v.PlateNumber).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Vehicle>> GetExpiryAlertsAsync(
        Guid tenantId, int withinDays = 30, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.Date.AddDays(withinDays);
        return await context.Vehicles
            .Where(v => v.TenantId == tenantId
                && v.RegistrationExpiry != null
                && v.RegistrationExpiry <= threshold
                && v.Status != VehicleStatus.Decommissioned)
            .OrderBy(v => v.RegistrationExpiry)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Vehicle entity, CancellationToken ct = default)
    {
        await context.Vehicles.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Vehicle entity, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Vehicle entity, CancellationToken ct = default)
    {
        context.Vehicles.Remove(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task AddMaintenanceRecordAsync(MaintenanceRecord record, CancellationToken ct = default)
    {
        await context.MaintenanceRecords.AddAsync(record, ct);
        await context.SaveChangesAsync(ct);
    }
}

public sealed class DriverRepository(ResourcesDbContext context) : IDriverRepository
{
    public async Task<Driver?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Driver?> GetByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default) =>
        await context.Drivers.FirstOrDefaultAsync(d => d.EmployeeCode == employeeCode, ct);

    public async Task<bool> ExistsByEmployeeCodeAsync(string employeeCode, CancellationToken ct = default) =>
        await context.Drivers.AnyAsync(d => d.EmployeeCode == employeeCode, ct);

    public async Task<(IReadOnlyList<Driver> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? status = null, string? licenseType = null, Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var query = context.Drivers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(d => d.Status.ToString() == status);
        if (!string.IsNullOrWhiteSpace(licenseType))
            query = query.Where(d => d.License.LicenseType == licenseType);
        if (tenantId.HasValue)
            query = query.Where(d => d.TenantId == tenantId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<Driver>> GetAvailableAsync(
        Guid tenantId, string? requiredLicenseType = null,
        CancellationToken ct = default)
    {
        var query = context.Drivers
            .Where(d => d.TenantId == tenantId
                && d.Status == DriverStatus.Available
                && d.License.ExpiryDate >= DateTime.UtcNow.Date);

        if (!string.IsNullOrWhiteSpace(requiredLicenseType))
            query = query.Where(d => d.License.LicenseType == requiredLicenseType);

        return await query.OrderByDescending(d => d.PerformanceScore).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Driver>> GetExpiryAlertsAsync(
        Guid tenantId, int withinDays = 30, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.Date.AddDays(withinDays);
        return await context.Drivers
            .Where(d => d.TenantId == tenantId
                && d.License.ExpiryDate <= threshold
                && d.Status != DriverStatus.Suspended)
            .OrderBy(d => d.License.ExpiryDate)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Driver entity, CancellationToken ct = default)
    {
        await context.Drivers.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Driver entity, CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Driver entity, CancellationToken ct = default)
    {
        context.Drivers.Remove(entity);
        await context.SaveChangesAsync(ct);
    }
}
