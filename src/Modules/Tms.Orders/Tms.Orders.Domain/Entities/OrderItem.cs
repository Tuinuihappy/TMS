using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? SKU { get; private set; }
    public decimal Weight { get; private set; }    // kg
    public decimal Volume { get; private set; }    // CBM
    public int Quantity { get; private set; }
    public bool IsDangerousGoods { get; private set; }
    public string? UNNumber { get; private set; }      // UN number for hazmat
    public string? DGClass { get; private set; }        // DG classification

    private OrderItem() { }  // EF Core

    public static OrderItem Create(
        Guid orderId,
        string description,
        decimal weight,
        decimal volume,
        int quantity,
        string? sku = null,
        bool isDangerousGoods = false,
        string? unNumber = null,
        string? dgClass = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (weight <= 0) throw new ArgumentException("Weight must be positive.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");

        if (isDangerousGoods && string.IsNullOrWhiteSpace(unNumber))
            throw new ArgumentException("UN Number is required for dangerous goods.", nameof(unNumber));

        return new OrderItem
        {
            OrderId = orderId,
            Description = description,
            SKU = sku,
            Weight = weight,
            Volume = volume,
            Quantity = quantity,
            IsDangerousGoods = isDangerousGoods,
            UNNumber = unNumber,
            DGClass = dgClass
        };
    }
}
