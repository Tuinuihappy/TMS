using Tms.SharedKernel.Application;

namespace Tms.Orders.Application.Features.CreateOrder;

public sealed record CreateOrderResponse(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal TotalWeight,
    decimal TotalVolumeCBM,
    DateTime CreatedAt);

public sealed record OrderItemDto(
    string Description,
    decimal WeightKg,
    decimal VolumeCBM,
    int Quantity,
    bool IsDangerousGoods = false,
    string? UNNumber = null,
    string? DGClass = null,
    string? Sku = null);

public sealed record AddressDto(
    string Street,
    string SubDistrict,
    string District,
    string Province,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null,
    string? Name = null);

public sealed record TimeWindowDto(DateTime From, DateTime To);

public sealed record OrderStopDto(
    AddressDto PickupAddress,
    AddressDto DropoffAddress,
    List<OrderItemDto> Items,
    int Sequence = 0,
    TimeWindowDto? PickupWindow = null,
    TimeWindowDto? DropoffWindow = null);

public sealed record CreateOrderCommand(
    Guid CustomerId,
    Guid TenantId,
    string? OrderNumber,
    List<OrderStopDto> Stops,
    string? Priority = "Normal",
    string? Notes = null
) : ICommand<CreateOrderResponse>;
