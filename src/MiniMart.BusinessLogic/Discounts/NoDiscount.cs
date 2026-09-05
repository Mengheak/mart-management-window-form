namespace MiniMart.BusinessLogic.Discounts;

public sealed class NoDiscount : DiscountStrategy
{
    public static NoDiscount Instance { get; } = new();

    public override string Description => "No discount";

    public override decimal Apply(decimal subtotal) => Round(subtotal);

    public override string ToString() => Description;
}
