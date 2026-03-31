using Tms.Resources.Domain.Entities;
using Tms.Resources.Domain.Interfaces;
using Tms.SharedKernel.Application;
using Tms.SharedKernel.Exceptions;
using Tms.SharedKernel.IntegrationEvents;

namespace Tms.Resources.Application.Features;

// ── Shared DTOs ─────────────────────────────────────────────────────────
public sealed record VehicleTypeDto(Guid Id, string Name, string Category, decimal MaxPayloadKg, decimal MaxVolumeCBM, string? RequiredLicenseType, bool HasRefrigeration);

public sealed record VehicleDto(Guid Id, string PlateNumber, Guid VehicleTypeId, string Status, string Ownership, string? SubcontractorName, decimal CurrentOdometerKm, DateTime? RegistrationExpiry, DateTime CreatedAt);

public sealed record DriverDto(Guid Id, string EmployeeCode, string FullName, string? PhoneNumber, string Status, string LicenseType, string LicenseNumber, DateTime LicenseExpiry, decimal PerformanceScore, DateTime CreatedAt);

public sealed record HOSRecordDto(Guid Id, DateOnly Date, decimal DrivingHours, decimal RestingHours, Guid? TripId, DateTime CreatedAt);

// ══════════════════════════════════════════════════════════════════════════
// VEHICLE COMMANDS
// ══════════════════════════════════════════════════════════════════════════

public sealed record CreateVehicleCommand(
    string PlateNumber, Guid VehicleTypeId, Guid TenantId,
    string Ownership = "Own",
    decimal CurrentOdometerKm = 0,
    DateTime? RegistrationExpiry = null,
    string? SubcontractorName = null) : ICommand<Guid>;

public sealed class CreateVehicleHandler(IVehicleRepository repo)
    : ICommandHandler<CreateVehicleCommand, Guid>
{
    public async Task<Guid> Handle(CreateVehicleCommand request, CancellationToken ct)
    {
        if (await repo.ExistsByPlateNumberAsync(request.PlateNumber, ct))
            throw new DomainException($"Vehicle with plate '{request.PlateNumber}' already exists.", "DUPLICATE_PLATE");

        var ownership = Enum.Parse<VehicleOwnership>(request.Ownership, ignoreCase: true);
        var vehicle = Vehicle.Create(
            request.PlateNumber, request.VehicleTypeId, request.TenantId,
            ownership, request.CurrentOdometerKm, request.RegistrationExpiry,
            request.SubcontractorName);

        await repo.AddAsync(vehicle, ct);
        return vehicle.Id;
    }
}

public sealed record ChangeVehicleStatusCommand(Guid VehicleId, string NewStatus) : ICommand;

public sealed class ChangeVehicleStatusHandler(
    IVehicleRepository repo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<ChangeVehicleStatusCommand>
{
    public async Task Handle(ChangeVehicleStatusCommand request, CancellationToken ct)
    {
        var vehicle = await repo.GetByIdAsync(request.VehicleId, ct)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        var oldStatus = vehicle.Status.ToString();
        vehicle.ChangeStatus(Enum.Parse<VehicleStatus>(request.NewStatus, ignoreCase: true));
        await repo.UpdateAsync(vehicle, ct);

        await eventPublisher.PublishAsync(
            new VehicleStatusChangedIntegrationEvent(
                vehicle.Id, vehicle.PlateNumber, oldStatus, vehicle.Status.ToString()), ct);
    }
}

// ── Update Vehicle ──────────────────────────────────────────────────────
public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string? SubcontractorName = null,
    DateTime? RegistrationExpiry = null,
    decimal? CurrentOdometerKm = null) : ICommand;

public sealed class UpdateVehicleHandler(IVehicleRepository repo)
    : ICommandHandler<UpdateVehicleCommand>
{
    public async Task Handle(UpdateVehicleCommand request, CancellationToken ct)
    {
        var vehicle = await repo.GetByIdAsync(request.VehicleId, ct)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        if (request.CurrentOdometerKm.HasValue)
            vehicle.UpdateOdometer(request.CurrentOdometerKm.Value);

        await repo.UpdateAsync(vehicle, ct);
    }
}

public sealed record AddMaintenanceCommand(
    Guid VehicleId, string Type, DateTime ScheduledDate,
    decimal? OdometerAtService = null, string? Notes = null) : ICommand;

public sealed class AddMaintenanceHandler(IVehicleRepository repo)
    : ICommandHandler<AddMaintenanceCommand>
{
    public async Task Handle(AddMaintenanceCommand request, CancellationToken ct)
    {
        var vehicle = await repo.GetByIdAsync(request.VehicleId, ct)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);
        var record = MaintenanceRecord.Create(
            vehicle.Id, request.Type, request.ScheduledDate,
            request.OdometerAtService, request.Notes);
        await repo.AddMaintenanceRecordAsync(record, ct);
    }
}

// ══════════════════════════════════════════════════════════════════════════
// VEHICLE QUERIES
// ══════════════════════════════════════════════════════════════════════════

public sealed record GetVehiclesQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null, Guid? VehicleTypeId = null, Guid? TenantId = null
) : IQuery<PagedResult<VehicleDto>>;

public sealed class GetVehiclesHandler(IVehicleRepository repo)
    : IQueryHandler<GetVehiclesQuery, PagedResult<VehicleDto>>
{
    public async Task<PagedResult<VehicleDto>> Handle(GetVehiclesQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.VehicleTypeId, request.TenantId, ct);

        return PagedResult<VehicleDto>.Create(
            items.Select(MapDto).ToList(),
            total, request.Page, request.PageSize);
    }

    internal static VehicleDto MapDto(Vehicle v) => new(
        v.Id, v.PlateNumber, v.VehicleTypeId, v.Status.ToString(),
        v.Ownership.ToString(), v.SubcontractorName, v.CurrentOdometerKm,
        v.RegistrationExpiry, v.CreatedAt);
}

public sealed record GetVehicleByIdQuery(Guid VehicleId) : IQuery<VehicleDto?>;
public sealed class GetVehicleByIdHandler(IVehicleRepository repo)
    : IQueryHandler<GetVehicleByIdQuery, VehicleDto?>
{
    public async Task<VehicleDto?> Handle(GetVehicleByIdQuery request, CancellationToken ct)
    {
        var v = await repo.GetByIdAsync(request.VehicleId, ct);
        return v is null ? null : GetVehiclesHandler.MapDto(v);
    }
}

public sealed record GetAvailableVehiclesQuery(Guid TenantId, decimal? MinPayloadKg = null) : IQuery<List<VehicleDto>>;
public sealed class GetAvailableVehiclesHandler(IVehicleRepository repo)
    : IQueryHandler<GetAvailableVehiclesQuery, List<VehicleDto>>
{
    public async Task<List<VehicleDto>> Handle(GetAvailableVehiclesQuery request, CancellationToken ct)
    {
        var items = await repo.GetAvailableAsync(request.TenantId, request.MinPayloadKg, ct: ct);
        return items.Select(GetVehiclesHandler.MapDto).ToList();
    }
}
// ── Vehicle Expiry Alerts ──────────────────────────────────────────────
public sealed record GetVehicleExpiryAlertsQuery(Guid TenantId, int WithinDays = 30) : IQuery<List<VehicleDto>>;
public sealed class GetVehicleExpiryAlertsHandler(IVehicleRepository repo)
    : IQueryHandler<GetVehicleExpiryAlertsQuery, List<VehicleDto>>
{
    public async Task<List<VehicleDto>> Handle(GetVehicleExpiryAlertsQuery request, CancellationToken ct)
    {
        var items = await repo.GetExpiryAlertsAsync(request.TenantId, request.WithinDays, ct);
        return items.Select(GetVehiclesHandler.MapDto).ToList();
    }
}
// ══════════════════════════════════════════════════════════════════════════
// DRIVER COMMANDS
// ══════════════════════════════════════════════════════════════════════════

public sealed record CreateDriverCommand(
    string EmployeeCode, string FullName, Guid TenantId,
    string LicenseNumber, string LicenseType, DateTime LicenseExpiryDate,
    string? PhoneNumber = null) : ICommand<Guid>;

public sealed class CreateDriverHandler(IDriverRepository repo)
    : ICommandHandler<CreateDriverCommand, Guid>
{
    public async Task<Guid> Handle(CreateDriverCommand request, CancellationToken ct)
    {
        if (await repo.ExistsByEmployeeCodeAsync(request.EmployeeCode, ct))
            throw new DomainException($"Driver with code '{request.EmployeeCode}' already exists.", "DUPLICATE_EMPLOYEE_CODE");

        var license = new LicenseInfo(request.LicenseNumber, request.LicenseType, request.LicenseExpiryDate);
        var driver = Driver.Create(request.EmployeeCode, request.FullName, request.TenantId, license, request.PhoneNumber);

        await repo.AddAsync(driver, ct);
        return driver.Id;
    }
}

public sealed record ChangeDriverStatusCommand(Guid DriverId, string NewStatus, string? SuspendReason = null) : ICommand;
public sealed class ChangeDriverStatusHandler(
    IDriverRepository repo,
    IIntegrationEventPublisher eventPublisher)
    : ICommandHandler<ChangeDriverStatusCommand>
{
    public async Task Handle(ChangeDriverStatusCommand request, CancellationToken ct)
    {
        var driver = await repo.GetByIdAsync(request.DriverId, ct)
            ?? throw new NotFoundException(nameof(Driver), request.DriverId);

        var oldStatus = driver.Status.ToString();

        if (request.NewStatus.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
            driver.Suspend(request.SuspendReason ?? "No reason provided");
        else
            driver.ChangeStatus(Enum.Parse<DriverStatus>(request.NewStatus, ignoreCase: true));

        await repo.UpdateAsync(driver, ct);

        await eventPublisher.PublishAsync(
            new DriverStatusChangedIntegrationEvent(
                driver.Id, driver.EmployeeCode, oldStatus, driver.Status.ToString()), ct);
    }
}

// ── Update Driver ──────────────────────────────────────────────────────
public sealed record UpdateDriverCommand(
    Guid DriverId,
    string? FullName = null,
    string? PhoneNumber = null,
    string? LicenseNumber = null,
    string? LicenseType = null,
    DateTime? LicenseExpiryDate = null) : ICommand;

public sealed class UpdateDriverHandler(IDriverRepository repo)
    : ICommandHandler<UpdateDriverCommand>
{
    public async Task Handle(UpdateDriverCommand request, CancellationToken ct)
    {
        var driver = await repo.GetByIdAsync(request.DriverId, ct)
            ?? throw new NotFoundException(nameof(Driver), request.DriverId);

        // Build new license if any license field changed
        LicenseInfo? newLicense = null;
        if (request.LicenseNumber is not null || request.LicenseType is not null || request.LicenseExpiryDate.HasValue)
        {
            var current = driver.License;
            newLicense = new LicenseInfo(
                request.LicenseNumber ?? current.LicenseNumber,
                request.LicenseType ?? current.LicenseType,
                request.LicenseExpiryDate ?? current.ExpiryDate);
        }

        driver.UpdateInfo(request.FullName, request.PhoneNumber, newLicense);
        await repo.UpdateAsync(driver, ct);
    }
}

// ══════════════════════════════════════════════════════════════════════════
// DRIVER QUERIES
// ══════════════════════════════════════════════════════════════════════════

public sealed record GetDriversQuery(
    int Page = 1, int PageSize = 20,
    string? Status = null, string? LicenseType = null, Guid? TenantId = null
) : IQuery<PagedResult<DriverDto>>;

public sealed class GetDriversHandler(IDriverRepository repo)
    : IQueryHandler<GetDriversQuery, PagedResult<DriverDto>>
{
    public async Task<PagedResult<DriverDto>> Handle(GetDriversQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            request.Page, request.PageSize,
            request.Status, request.LicenseType, request.TenantId, ct);

        return PagedResult<DriverDto>.Create(
            items.Select(MapDto).ToList(), total, request.Page, request.PageSize);
    }

    internal static DriverDto MapDto(Driver d) => new(
        d.Id, d.EmployeeCode, d.FullName, d.PhoneNumber,
        d.Status.ToString(), d.License.LicenseType, d.License.LicenseNumber,
        d.License.ExpiryDate, d.PerformanceScore, d.CreatedAt);
}

public sealed record GetDriverByIdQuery(Guid DriverId) : IQuery<DriverDto?>;
public sealed class GetDriverByIdHandler(IDriverRepository repo)
    : IQueryHandler<GetDriverByIdQuery, DriverDto?>
{
    public async Task<DriverDto?> Handle(GetDriverByIdQuery request, CancellationToken ct)
    {
        var d = await repo.GetByIdAsync(request.DriverId, ct);
        return d is null ? null : GetDriversHandler.MapDto(d);
    }
}

public sealed record GetAvailableDriversQuery(Guid TenantId, string? RequiredLicenseType = null) : IQuery<List<DriverDto>>;
public sealed class GetAvailableDriversHandler(IDriverRepository repo)
    : IQueryHandler<GetAvailableDriversQuery, List<DriverDto>>
{
    public async Task<List<DriverDto>> Handle(GetAvailableDriversQuery request, CancellationToken ct)
    {
        var items = await repo.GetAvailableAsync(request.TenantId, request.RequiredLicenseType, ct);
        return items.Select(GetDriversHandler.MapDto).ToList();
    }
}

// ── Driver Expiry Alerts ──────────────────────────────────────────────
public sealed record GetDriverExpiryAlertsQuery(Guid TenantId, int WithinDays = 30) : IQuery<List<DriverDto>>;
public sealed class GetDriverExpiryAlertsHandler(IDriverRepository repo)
    : IQueryHandler<GetDriverExpiryAlertsQuery, List<DriverDto>>
{
    public async Task<List<DriverDto>> Handle(GetDriverExpiryAlertsQuery request, CancellationToken ct)
    {
        var items = await repo.GetExpiryAlertsAsync(request.TenantId, request.WithinDays, ct);
        return items.Select(GetDriversHandler.MapDto).ToList();
    }
}

// ── Driver HOS History ────────────────────────────────────────────────
public sealed record GetDriverHOSQuery(Guid DriverId, DateOnly? Date = null) : IQuery<List<HOSRecordDto>>;
public sealed class GetDriverHOSHandler(IDriverRepository repo)
    : IQueryHandler<GetDriverHOSQuery, List<HOSRecordDto>>
{
    public async Task<List<HOSRecordDto>> Handle(GetDriverHOSQuery request, CancellationToken ct)
    {
        var driver = await repo.GetByIdAsync(request.DriverId, ct)
            ?? throw new NotFoundException(nameof(Driver), request.DriverId);

        var records = driver.HOSHistory.AsEnumerable();
        if (request.Date.HasValue)
            records = records.Where(h => h.Date == request.Date.Value);

        return records.OrderByDescending(h => h.Date)
            .Select(h => new HOSRecordDto(h.Id, h.Date, h.DrivingHours, h.RestingHours, h.TripId, h.CreatedAt))
            .ToList();
    }
}
