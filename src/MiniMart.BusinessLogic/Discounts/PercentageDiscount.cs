using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Discounts;

public sealed class PercentageDiscount : DiscountStrategy
{
    public decimal Percentage { get; }

    public PercentageDiscount(decimal percentage)
    {
        if (percentage < 0m || percentage > 100m)
        {
            throw new BusinessRuleException("Discount percentage must be between 0 and 100.");
        }

        Percentage = percentage;
    }

    public override string Description => $"{Percentage:0.##}% discount";

    public override decimal Apply(decimal subtotal)
    {
        if (subtotal <= 0m)
        {
            return 0m;
        }

        return Round(subtotal * (1m - (Percentage / 100m)));
    }

    public override string ToString() => Description;
}
