using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class CartItem
{
    public Product Product { get; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; }

    public int ProductId => Product.ProductId;

    public string ProductName => Product.Name;

    public string Sku => Product.Sku;

    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    public CartItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        Product = product;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public void AddQuantity(int additionalQuantity)
    {
        if (additionalQuantity <= 0)
        {
            throw new BusinessRuleException("Quantity to add must be greater than zero.");
        }

        Quantity += additionalQuantity;
    }
}
