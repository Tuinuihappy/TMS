using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Weight { get; private set; }    // kg
    public decimal Volume { get; private set; }    // CBM
    public int Quantity { get; private set; }

    private OrderItem() { }  // EF Core

    public static OrderItem Create(
        Guid orderId,
        string description,
        decimal weight,
        decimal volume,
        int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (weight <= 0) throw new ArgumentException("Weight must be positive.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");

        return new OrderItem
        {
            OrderId = orderId,
            Description = description,
            Weight = weight,
            Volume = volume,
            Quantity = quantity
        };
    }
}
