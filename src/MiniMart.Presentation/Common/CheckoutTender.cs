namespace MiniMart.Presentation.Common;

// A sale remains denominated in USD. KHR tender is converted only for checkout.
public sealed class CheckoutTender
{
    public decimal TotalUsd { get; }
    public decimal UsdPaid { get; }
    public decimal KhrPaid { get; }
    public decimal KhrPerUsd { get; }

    public decimal TotalKhr => RoundRiel(TotalUsd * KhrPerUsd);
    public decimal PaidKhrEquivalent => RoundRiel(UsdPaid * KhrPerUsd) + KhrPaid;
    public bool IsSufficient => PaidKhrEquivalent >= TotalKhr;
    public decimal ChangeKhr => IsSufficient ? PaidKhrEquivalent - TotalKhr : 0m;
    public decimal OutstandingKhr => IsSufficient ? 0m : TotalKhr - PaidKhrEquivalent;

    // The existing sale record stores two decimal places in USD. Round the
    // converted cash once, and never store less than the settled USD total.
    public decimal AmountPaidUsd => IsSufficient
        ? Math.Max(TotalUsd, decimal.Round(UsdPaid + KhrPaid / KhrPerUsd, 2, MidpointRounding.AwayFromZero))
        : decimal.Round(UsdPaid + KhrPaid / KhrPerUsd, 2, MidpointRounding.AwayFromZero);

    public CheckoutTender(decimal totalUsd, decimal usdPaid, decimal khrPaid, decimal khrPerUsd)
    {
        if (totalUsd < 0m || usdPaid < 0m || khrPaid < 0m || khrPerUsd <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(khrPerUsd), "Amounts must be nonnegative and the rate must be positive.");
        }

        TotalUsd = totalUsd;
        UsdPaid = usdPaid;
        KhrPaid = khrPaid;
        KhrPerUsd = khrPerUsd;
    }

    private static decimal RoundRiel(decimal amount) =>
        decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
}
