namespace Tms.Orders.Domain.ValueObjects;

public sealed record Weight(decimal Value, Enums.WeightUnit Unit)
{
    public static Weight Create(decimal value, Enums.WeightUnit unit = Enums.WeightUnit.Kg)
    {
        if (value < 0) throw new ArgumentException("Weight value cannot be negative.");
        return new(value, unit);
    }

    public decimal ToKg() => Unit == Enums.WeightUnit.Ton ? Value * 1000m : Value;

    public override string ToString() => $"{Value} {Unit}";
}
