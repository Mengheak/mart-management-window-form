using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Discounts;

public sealed class FlatDiscount : DiscountStrategy
{
    public decimal Amount { get; }

    public FlatDiscount(decimal amount)
    {
        if (amount < 0m)
        {
            throw new BusinessRuleException("Discount amount cannot be negative.");
        }

        Amount = Round(amount);
    }

    public override string Description => $"{Amount:N2} off";

    public override decimal Apply(decimal subtotal)
    {
        if (subtotal <= 0m)
        {
            return 0m;
        }

        var payable = Round(subtotal) - Amount;
        return payable < 0m ? 0m : Round(payable);
    }

    public override string ToString() => Description;
}
