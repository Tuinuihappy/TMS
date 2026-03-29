using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Resources.Domain.Entities;

public enum VehicleStatus { Available, Assigned, InUse, InRepair, Decommissioned }
public enum VehicleOwnership { Own, Subcontract }
public enum DriverStatus { Available, OnDuty, OffDuty, OnLeave, Suspended }

// ── Vehicle Type (referenced by Vehicle) ─────────────────────────────────
public sealed class VehicleType : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal MaxPayloadKg { get; private set; }
    public decimal MaxVolumeCBM { get; private set; }
    public string? RequiredLicenseType { get; private set; }
    public bool HasRefrigeration { get; private set; }
    public Guid TenantId { get; private set; }

    private VehicleType() { }

    public static VehicleType Create(
        string name, string category, decimal maxPayloadKg, decimal maxVolumeCBM,
        Guid tenantId, string? requiredLicenseType = null, bool hasRefrigeration = false) =>
        new()
        {
            Name = name, Category = category,
            MaxPayloadKg = maxPayloadKg, MaxVolumeCBM = maxVolumeCBM,
            RequiredLicenseType = requiredLicenseType,
            HasRefrigeration = hasRefrigeration, TenantId = tenantId
        };
}

// ── Vehicle Aggregate ─────────────────────────────────────────────────────
public sealed class Vehicle : AggregateRoot
{
    public string PlateNumber { get; private set; } = string.Empty;
    public Guid VehicleTypeId { get; private set; }
    public VehicleStatus Status { get; private set; }
    public VehicleOwnership Ownership { get; private set; }
    public string? SubcontractorName { get; private set; }
    public decimal CurrentOdometerKm { get; private set; }
    public DateTime? RegistrationExpiry { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<MaintenanceRecord> _maintenanceRecords = [];
    public IReadOnlyCollection<MaintenanceRecord> MaintenanceRecords => _maintenanceRecords.AsReadOnly();

    private readonly List<InsuranceRecord> _insuranceRecords = [];
    public IReadOnlyCollection<InsuranceRecord> InsuranceRecords => _insuranceRecords.AsReadOnly();

    private Vehicle() { }

    public static Vehicle Create(
        string plateNumber, Guid vehicleTypeId, Guid tenantId,
        VehicleOwnership ownership = VehicleOwnership.Own,
        decimal currentOdometerKm = 0,
        DateTime? registrationExpiry = null,
        string? subcontractorName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plateNumber);
        return new Vehicle
        {
            PlateNumber = plateNumber, VehicleTypeId = vehicleTypeId,
            TenantId = tenantId, Ownership = ownership,
            CurrentOdometerKm = currentOdometerKm,
            RegistrationExpiry = registrationExpiry,
            SubcontractorName = subcontractorName,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Rule 3: RegistrationExpiry < today → block assignment</summary>
    public bool IsRegistrationValid() =>
        RegistrationExpiry is null || RegistrationExpiry >= DateTime.UtcNow.Date;

    public void ChangeStatus(VehicleStatus newStatus)
    {
        Status = newStatus;
    }

    public void UpdateOdometer(decimal km)
    {
        if (km < CurrentOdometerKm)
            throw new DomainException("Odometer cannot decrease.", "INVALID_ODOMETER");
        CurrentOdometerKm = km;
    }

    public void AddMaintenance(MaintenanceRecord record)
    {
        _maintenanceRecords.Add(record);
        Status = VehicleStatus.InRepair;
    }

    public void CompleteMaintenance(Guid recordId)
    {
        var record = _maintenanceRecords.FirstOrDefault(m => m.Id == recordId)
            ?? throw new NotFoundException(nameof(MaintenanceRecord), recordId);
        record.Complete();
        if (!_maintenanceRecords.Any(m => m.CompletedDate is null))
            Status = VehicleStatus.Available;
    }
}

// ── Supporting Entities ───────────────────────────────────────────────────
public sealed class MaintenanceRecord : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public DateTime ScheduledDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public decimal? OdometerAtService { get; private set; }
    public decimal? Cost { get; private set; }
    public string? Notes { get; private set; }

    private MaintenanceRecord() { }

    public static MaintenanceRecord Create(
        Guid vehicleId, string type, DateTime scheduledDate,
        decimal? odometer = null, string? notes = null) =>
        new()
        {
            VehicleId = vehicleId, Type = type,
            ScheduledDate = scheduledDate,
            OdometerAtService = odometer, Notes = notes
        };

    public void Complete(decimal? cost = null)
    {
        CompletedDate = DateTime.UtcNow;
        Cost = cost;
    }
}

public sealed class InsuranceRecord : BaseEntity
{
    public Guid VehicleId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? PolicyNumber { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public string? Provider { get; private set; }

    private InsuranceRecord() { }

    public static InsuranceRecord Create(
        Guid vehicleId, string type, DateTime startDate, DateTime expiryDate,
        string? policyNumber = null, string? provider = null) =>
        new()
        {
            VehicleId = vehicleId, Type = type,
            PolicyNumber = policyNumber, StartDate = startDate,
            ExpiryDate = expiryDate, Provider = provider
        };

    public bool IsExpired() => ExpiryDate < DateTime.UtcNow.Date;
    public bool ExpiresWithin(int days) => ExpiryDate <= DateTime.UtcNow.Date.AddDays(days);
}

// ── Driver Value Objects ───────────────────────────────────────────────────
public sealed record LicenseInfo(
    string LicenseNumber,
    string LicenseType,
    DateTime ExpiryDate)
{
    private static readonly Dictionary<string, IReadOnlyList<string>> AllowedTypes = new()
    {
        ["ท.1"] = ["4ล้อ"],
        ["ท.2"] = ["4ล้อ", "6ล้อ"],
        ["ท.3"] = ["4ล้อ", "6ล้อ", "10ล้อ"],
        ["ท.4"] = ["4ล้อ", "6ล้อ", "10ล้อ", "หัวลาก"]
    };

    public bool IsExpired() => ExpiryDate < DateTime.UtcNow.Date;
    public bool ExpiresWithin(int days) => ExpiryDate <= DateTime.UtcNow.Date.AddDays(days);

    public bool CanDrive(string vehicleCategory) =>
        AllowedTypes.TryGetValue(LicenseType, out var allowed) &&
        allowed.Any(a => vehicleCategory.Contains(a, StringComparison.OrdinalIgnoreCase));
}

// ── Driver Aggregate ───────────────────────────────────────────────────────
public sealed class Driver : AggregateRoot
{
    public string EmployeeCode { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public DriverStatus Status { get; private set; }
    public LicenseInfo License { get; private set; } = null!;
    public decimal PerformanceScore { get; private set; } = 5.0m;
    public string? SuspendReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid TenantId { get; private set; }

    private readonly List<HOSRecord> _hosHistory = [];
    public IReadOnlyCollection<HOSRecord> HOSHistory => _hosHistory.AsReadOnly();

    private Driver() { }

    public static Driver Create(
        string employeeCode, string fullName, Guid tenantId,
        LicenseInfo license, string? phoneNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new Driver
        {
            EmployeeCode = employeeCode, FullName = fullName,
            TenantId = tenantId, License = license,
            PhoneNumber = phoneNumber,
            Status = DriverStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Rule 2: License expired → block</summary>
    public void EnsureLicenseValid()
    {
        if (License.IsExpired())
            throw new DomainException($"Driver license expired on {License.ExpiryDate:yyyy-MM-dd}.", "LICENSE_EXPIRED");
    }

    /// <summary>Rule 5: Daily HOS ≤ 10 hours</summary>
    public bool IsAvailableForDuration(int estimatedMinutes)
    {
        var todayHours = _hosHistory
            .Where(h => h.Date == DateOnly.FromDateTime(DateTime.UtcNow))
            .Sum(h => h.DrivingHours);
        return (todayHours + estimatedMinutes / 60m) <= 10m;
    }

    public void RecordHOS(decimal drivingHours, decimal restingHours, Guid? tripId = null)
    {
        _hosHistory.Add(HOSRecord.Create(Id, drivingHours, restingHours, tripId));
    }

    public void UpdatePerformance(decimal score)
    {
        if (score < 0 || score > 5)
            throw new ArgumentException("Score must be between 0 and 5.");
        PerformanceScore = score;
    }

    public void Activate() => Status = DriverStatus.Available;

    public void Suspend(string reason)
    {
        SuspendReason = reason;
        Status = DriverStatus.Suspended;
    }

    public void ChangeStatus(DriverStatus newStatus) => Status = newStatus;
}

public sealed class HOSRecord : BaseEntity
{
    public Guid DriverId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal DrivingHours { get; private set; }
    public decimal RestingHours { get; private set; }
    public Guid? TripId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private HOSRecord() { }

    public static HOSRecord Create(
        Guid driverId, decimal drivingHours, decimal restingHours, Guid? tripId = null) =>
        new()
        {
            DriverId = driverId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            DrivingHours = drivingHours,
            RestingHours = restingHours,
            TripId = tripId,
            CreatedAt = DateTime.UtcNow
        };
}
