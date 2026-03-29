using Tms.Platform.Domain.Enums;
using Tms.SharedKernel.Domain;
using Tms.SharedKernel.Exceptions;

namespace Tms.Platform.Domain.Entities;

public sealed class Location : AggregateRoot
{
    public string LocationCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? AddressLine { get; private set; }
    public string? District { get; private set; }
    public string? Province { get; private set; }
    public string? PostalCode { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string? Zone { get; private set; }
    public LocationType Type { get; private set; }
    public Guid? CustomerId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid TenantId { get; private set; }

    private Location() { }

    public static Location Create(
        string locationCode, string name, double latitude, double longitude,
        LocationType type, Guid tenantId,
        string? addressLine = null, string? district = null,
        string? province = null, string? postalCode = null,
        string? zone = null, Guid? customerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Location
        {
            LocationCode = locationCode, Name = name,
            Latitude = latitude, Longitude = longitude,
            Type = type, TenantId = tenantId,
            AddressLine = addressLine, District = district,
            Province = province, PostalCode = postalCode,
            Zone = zone, CustomerId = customerId, IsActive = true
        };
    }

    public void Update(string name, string? addressLine, string? province,
        string? zone, double latitude, double longitude)
    {
        Name = name; AddressLine = addressLine;
        Province = province; Zone = zone;
        Latitude = latitude; Longitude = longitude;
    }

    public void Deactivate() => IsActive = false;
}

public sealed class ReasonCode : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ReasonCategory Category { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid TenantId { get; private set; }

    private ReasonCode() { }

    public static ReasonCode Create(
        string code, string description, ReasonCategory category, Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new ReasonCode
        {
            Code = code, Description = description,
            Category = category, TenantId = tenantId, IsActive = true
        };
    }
}

public sealed class Province : BaseEntity
{
    public new int Id { get; private set; }
    public string NameTH { get; private set; } = string.Empty;
    public string? NameEN { get; private set; }
    public string? Region { get; private set; }

    private Province() { }

    public static Province Create(int id, string nameTh, string? nameEn, string? region) =>
        new() { Id = id, NameTH = nameTh, NameEN = nameEn, Region = region };
}

public sealed class Holiday : AggregateRoot
{
    public DateTime Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public Guid TenantId { get; private set; }

    private Holiday() { }

    public static Holiday Create(DateTime date, string description, Guid tenantId) =>
        new() { Date = date.Date, Description = description,
                Year = date.Year, TenantId = tenantId };
}
