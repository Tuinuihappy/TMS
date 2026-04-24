using Tms.Orders.Domain.Enums;
using Tms.Orders.Domain.ValueObjects;
using Tms.SharedKernel.Domain;

namespace Tms.Orders.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? SKU { get; private set; }
    public Weight Weight { get; private set; } = null!;
    public decimal VolumeCBM { get; private set; }
    public int Quantity { get; private set; }
    public bool IsDangerousGoods { get; private set; }
    public string? UNNumber { get; private set; }
    public string? DGClass { get; private set; }

    private OrderItem() { }  // EF Core

    public static OrderItem Create(
        Guid orderId,
        string description,
        decimal weightKg,
        decimal volumeCbm,
        int quantity,
        WeightUnit weightUnit = WeightUnit.Kg,
        string? sku = null,
        bool isDangerousGoods = false,
        string? unNumber = null,
        string? dgClass = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (weightKg <= 0) throw new ArgumentException("Weight must be positive.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");

        if (isDangerousGoods && string.IsNullOrWhiteSpace(dgClass))
            throw new ArgumentException("DG Class is required for dangerous goods.", nameof(dgClass));

        return new OrderItem
        {
            OrderId = orderId,
            Description = description,
            SKU = sku,
            Weight = Weight.Create(weightKg, weightUnit),
            VolumeCBM = volumeCbm,
            Quantity = quantity,
            IsDangerousGoods = isDangerousGoods,
            UNNumber = unNumber,
            DGClass = dgClass
        };
    }
}
