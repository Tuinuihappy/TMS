using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed record OrderItemDto(
    string Description,
    decimal Weight,
    decimal Volume,
    int Quantity);

public sealed record AddressDto(
    string Street,
    string SubDistrict,
    string District,
    string Province,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null);

public sealed record TimeWindowDto(DateTime From, DateTime To);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    string? OrderNumber,
    AddressDto PickupAddress,
    AddressDto DropoffAddress,
    List<OrderItemDto> Items,
    TimeWindowDto? PickupWindow = null,
    TimeWindowDto? DropoffWindow = null,
    string? Priority = "Normal",
    string? Notes = null
) : ICommand<Guid>;
