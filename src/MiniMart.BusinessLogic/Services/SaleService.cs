using MiniMart.BusinessLogic.Discounts;
using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;

namespace MiniMart.BusinessLogic.Services;

public class SaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;

    public DiscountStrategy DiscountStrategy { get; private set; } = NoDiscount.Instance;

    public SaleService(ISaleRepository saleRepository, IProductRepository productRepository)
    {
        _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public void ApplyDiscount(DiscountStrategy? strategy) =>
        DiscountStrategy = strategy ?? NoDiscount.Instance;

    public CheckoutQuote QuoteCart(Cart cart, decimal amountPaid = 0m)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var subtotal = cart.Subtotal;
        var total = DiscountStrategy.Apply(subtotal);
        var discountAmount = decimal.Round(subtotal - total, 2, MidpointRounding.AwayFromZero);
        var paid = decimal.Round(amountPaid, 2, MidpointRounding.AwayFromZero);
        var sufficient = paid >= total;

        return new CheckoutQuote(
            subtotal,
            discountAmount,
            total,
            paid,
            sufficient ? decimal.Round(paid - total, 2, MidpointRounding.AwayFromZero) : 0m,
            sufficient,
            DiscountStrategy.Description);
    }

    public async Task<Sale> CheckoutAsync(
        Cart cart,
        int cashierId,
        decimal amountPaid,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cart);

        if (cart.IsEmpty)
        {
            throw new BusinessRuleException("The cart is empty. Add at least one item before checking out.");
        }

        if (cashierId <= 0)
        {
            throw new BusinessRuleException("The sale could not be attributed to a signed-in cashier.");
        }

        await ValidateStockAsync(cart, cancellationToken).ConfigureAwait(false);

        var quote = QuoteCart(cart, amountPaid);

        if (!quote.IsPaymentSufficient)
        {
            throw new BusinessRuleException(
                $"Amount paid ({quote.AmountPaid:N2}) is less than the total due ({quote.Total:N2}). " +
                $"Collect a further {quote.AmountOutstanding:N2}.");
        }

        var sale = new Sale(cashierId, quote.Total, quote.AmountPaid);
        sale.AddLines(cart.Items.Select(SaleDetail.FromCartItem));

        return await _saleRepository.SaveSaleAsync(sale, cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateStockAsync(Cart cart, CancellationToken cancellationToken)
    {
        foreach (var line in cart.Items)
        {
            var current = await _productRepository
                .GetByIdAsync(line.ProductId, cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                throw new BusinessRuleException(
                    $"'{line.ProductName}' is no longer in the catalogue and cannot be sold.");
            }

            if (!current.IsActive)
            {
                throw new BusinessRuleException($"'{current.Name}' is discontinued and cannot be sold.");
            }

            if (!current.HasSufficientStock(line.Quantity))
            {
                throw new InsufficientStockException(current.Name, line.Quantity, current.StockQuantity);
            }
        }
    }
}
