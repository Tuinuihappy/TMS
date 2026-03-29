using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Resources.Domain.Entities;

public enum VehicleStatus { Available, Assigned, InUse, InRepair, Decommissioned }
public enum DriverStatus { Available, OnDuty, OffDuty, OnLeave, Suspended }

public sealed class Vehicle : AggregateRoot
{
    public string LicensePlate { get; private set; } = string.Empty;
    public string VehicleType { get; private set; } = string.Empty;
    public decimal PayloadKg { get; private set; }
    public decimal VolumeCbm { get; private set; }
    public VehicleStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Vehicle() { }

    public static Vehicle Create(string licensePlate, string vehicleType, decimal payloadKg, decimal volumeCbm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);
        return new Vehicle
        {
            LicensePlate = licensePlate,
            VehicleType = vehicleType,
            PayloadKg = payloadKg,
            VolumeCbm = volumeCbm,
            Status = VehicleStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Assign()
    {
        if (Status != VehicleStatus.Available)
            throw new DomainException($"Vehicle is not available (current: {Status}).");
        Status = VehicleStatus.Assigned;
    }

    public void Release() => Status = VehicleStatus.Available;
}

public sealed class Driver : AggregateRoot
{
    public string FullName { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public string LicenseType { get; private set; } = string.Empty;
    public DateTime LicenseExpiryDate { get; private set; }
    public DriverStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Driver() { }

    public static Driver Create(
        string fullName, string licenseNumber,
        string licenseType, DateTime licenseExpiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        return new Driver
        {
            FullName = fullName,
            LicenseNumber = licenseNumber,
            LicenseType = licenseType,
            LicenseExpiryDate = licenseExpiryDate,
            Status = DriverStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsLicenseValid() => LicenseExpiryDate > DateTime.UtcNow;
}
