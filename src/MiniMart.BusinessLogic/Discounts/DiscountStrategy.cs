namespace MiniMart.BusinessLogic.Discounts;

public abstract class DiscountStrategy
{
    public abstract string Description { get; }

    public abstract decimal Apply(decimal subtotal);

    public decimal CalculateDiscountAmount(decimal subtotal) =>
        Round(subtotal) - Apply(subtotal);

    protected static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
