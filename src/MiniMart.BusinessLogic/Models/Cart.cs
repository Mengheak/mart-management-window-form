using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class Cart
{
    private readonly List<CartItem> _items = new();

    public IReadOnlyList<CartItem> Items => _items;

    public bool IsEmpty => _items.Count == 0;

    public int LineCount => _items.Count;

    public int TotalUnits => _items.Sum(item => item.Quantity);

    public decimal Subtotal =>
        decimal.Round(_items.Sum(item => item.LineTotal), 2, MidpointRounding.AwayFromZero);

    public void AddItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        if (!product.IsActive)
        {
            throw new BusinessRuleException($"'{product.Name}' is discontinued and cannot be sold.");
        }

        var existing = _items.FirstOrDefault(item => item.ProductId == product.ProductId);
        var resultingQuantity = (existing?.Quantity ?? 0) + quantity;

        if (resultingQuantity > product.StockQuantity)
        {
            throw new InsufficientStockException(product.Name, resultingQuantity, product.StockQuantity);
        }

        if (existing is null)
        {
            _items.Add(new CartItem(product, quantity));
        }
        else
        {
            existing.AddQuantity(quantity);
        }
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var line = _items.FirstOrDefault(item => item.ProductId == productId)
            ?? throw new BusinessRuleException("That product is not in the cart.");

        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity must be greater than zero.");
        }

        if (quantity > line.Product.StockQuantity)
        {
            throw new InsufficientStockException(line.ProductName, quantity, line.Product.StockQuantity);
        }

        line.ChangeQuantity(quantity);
    }

    public bool RemoveItem(int productId) => _items.RemoveAll(item => item.ProductId == productId) > 0;

    public void Clear() => _items.Clear();
}
