using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class SaleDetail
{
    public int SaleDetailId { get; private set; }

    public int SaleId { get; private set; }

    public int ProductId { get; }

    public int Quantity { get; }

    public decimal UnitPrice { get; }

    public string? ProductName { get; private set; }

    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    public SaleDetail(int productId, int quantity, decimal unitPrice)
    {
        if (productId <= 0)
        {
            throw new BusinessRuleException("A sale line must reference a valid product.");
        }

        if (quantity <= 0)
        {
            throw new BusinessRuleException("Sale line quantity must be greater than zero.");
        }

        if (unitPrice < 0m)
        {
            throw new BusinessRuleException("Sale line unit price cannot be negative.");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    public static SaleDetail FromCartItem(CartItem cartItem)
    {
        ArgumentNullException.ThrowIfNull(cartItem);

        return new SaleDetail(cartItem.ProductId, cartItem.Quantity, cartItem.UnitPrice)
        {
            ProductName = cartItem.ProductName
        };
    }

    public static SaleDetail FromDatabase(
        int saleDetailId,
        int saleId,
        int productId,
        int quantity,
        decimal unitPrice,
        string? productName = null)
    {
        return new SaleDetail(productId, quantity, unitPrice)
        {
            SaleDetailId = saleDetailId,
            SaleId = saleId,
            ProductName = productName
        };
    }

    public void AssignSaleId(int saleId) => SaleId = saleId;

    public void AssignIdentity(int saleDetailId) => SaleDetailId = saleDetailId;
}
