using Tms.SharedKernel.Domain;
using Tms.Execution.Domain.Enums;

namespace Tms.Execution.Domain.Entities;

public sealed class ShipmentItem : BaseEntity
{
    public Guid ShipmentId { get; private set; }
    public string? SKU { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int ExpectedQty { get; private set; }
    public int DeliveredQty { get; private set; }
    public int ReturnedQty { get; private set; }
    public ShipmentItemStatus Status { get; private set; }
    public string? Barcode { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? LotNumber { get; private set; }

    private ShipmentItem() { } // EF Core

    public static ShipmentItem Create(
        Guid shipmentId, string description, int expectedQty,
        string? sku = null, string? barcode = null,
        string? serialNumber = null, string? lotNumber = null)
    {
        if (expectedQty <= 0)
            throw new ArgumentException("Expected quantity must be greater than 0.");

        return new ShipmentItem
        {
            ShipmentId    = shipmentId,
            SKU           = sku,
            Description   = description,
            ExpectedQty   = expectedQty,
            Barcode       = barcode,
            SerialNumber  = serialNumber,
            LotNumber     = lotNumber,
            DeliveredQty  = 0,
            ReturnedQty   = 0,
            Status        = ShipmentItemStatus.Pending
        };
    }

    internal void SetDelivered(int deliveredQty)
    {
        if (deliveredQty < 0 || deliveredQty > ExpectedQty)
            throw new ArgumentException($"Delivered qty must be between 0 and {ExpectedQty}.");

        DeliveredQty = deliveredQty;
        ReturnedQty = ExpectedQty - deliveredQty;
        Status = deliveredQty == ExpectedQty
            ? ShipmentItemStatus.Delivered
            : deliveredQty == 0
                ? ShipmentItemStatus.Returned
                : ShipmentItemStatus.Delivered; // partial = still Delivered
    }

    internal void SetReturned()
    {
        ReturnedQty = ExpectedQty;
        Status = ShipmentItemStatus.Returned;
    }
}
