using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.ValueObjects;

public sealed record Address(
    string Street,
    string SubDistrict,
    string District,
    string Province,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null,
    string? Name = null)
{
    public static Address Create(
        string street, string subDistrict, string district,
        string province, string postalCode,
        double? latitude = null, double? longitude = null,
        string? name = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(province);
        return new Address(street, subDistrict, district, province, postalCode, latitude, longitude, name);
    }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name)
            ? $"{Street}, {District}, {Province} {PostalCode}"
            : $"{Name} — {Street}, {District}, {Province} {PostalCode}";
}
