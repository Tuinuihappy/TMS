using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.ValueObjects;

public sealed record Address(
    string Street,
    string SubDistrict,
    string District,
    string Province,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null)
{
    public static Address Create(
        string street, string subDistrict, string district,
        string province, string postalCode,
        double? latitude = null, double? longitude = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(province);
        return new Address(street, subDistrict, district, province, postalCode, latitude, longitude);
    }

    public override string ToString() =>
        $"{Street}, {SubDistrict}, {District}, {Province} {PostalCode}";
}
