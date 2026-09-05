namespace MiniMart.BusinessLogic.Services;

public sealed record CheckoutQuote(
    decimal Subtotal,
    decimal DiscountAmount,
    decimal Total,
    decimal AmountPaid,
    decimal ChangeDue,
    bool IsPaymentSufficient,
    string DiscountDescription)
{
    public decimal AmountOutstanding =>
        IsPaymentSufficient ? 0m : decimal.Round(Total - AmountPaid, 2, MidpointRounding.AwayFromZero);
}
