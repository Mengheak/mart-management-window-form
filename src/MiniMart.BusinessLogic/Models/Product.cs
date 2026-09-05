using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class Product
{
    public const int NameMaxLength = 150;

    public const int SkuMaxLength = 50;

    private string _name = string.Empty;
    private string _sku = string.Empty;
    private decimal _unitPrice;
    private decimal _costPrice;
    private int _reorderLevel;
    private int _categoryId;

    public int ProductId { get; private set; }

    public string Name
    {
        get => _name;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("Product name is required.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > NameMaxLength)
            {
                throw new BusinessRuleException($"Product name cannot exceed {NameMaxLength} characters.");
            }

            _name = trimmed;
        }
    }

    public string Sku
    {
        get => _sku;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BusinessRuleException("SKU is required.");
            }

            var trimmed = value.Trim();
            if (trimmed.Length > SkuMaxLength)
            {
                throw new BusinessRuleException($"SKU cannot exceed {SkuMaxLength} characters.");
            }

            _sku = trimmed;
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        private set
        {
            if (value < 0m)
            {
                throw new BusinessRuleException("Unit price cannot be negative.");
            }

            _unitPrice = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    public decimal CostPrice
    {
        get => _costPrice;
        private set
        {
            if (value < 0m)
            {
                throw new BusinessRuleException("Cost price cannot be negative.");
            }

            _costPrice = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }

    public int StockQuantity { get; private set; }

    public int ReorderLevel
    {
        get => _reorderLevel;
        private set
        {
            if (value < 0)
            {
                throw new BusinessRuleException("Reorder level cannot be negative.");
            }

            _reorderLevel = value;
        }
    }

    public int CategoryId
    {
        get => _categoryId;
        private set
        {
            if (value <= 0)
            {
                throw new BusinessRuleException("A product must belong to a category.");
            }

            _categoryId = value;
        }
    }

    public bool IsActive { get; private set; } = true;

    public string? CategoryName { get; private set; }

    public bool IsLowStock => StockQuantity <= ReorderLevel;

    public decimal StockValue => UnitPrice * StockQuantity;

    public Product(
        string name,
        string sku,
        decimal unitPrice,
        decimal costPrice,
        int stockQuantity,
        int reorderLevel,
        int categoryId,
        bool isActive = true)
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        ReorderLevel = reorderLevel;
        CategoryId = categoryId;
        IsActive = isActive;

        if (stockQuantity < 0)
        {
            throw new BusinessRuleException("Opening stock quantity cannot be negative.");
        }

        StockQuantity = stockQuantity;
    }

    public static Product FromDatabase(
        int productId,
        string name,
        string sku,
        decimal unitPrice,
        decimal costPrice,
        int stockQuantity,
        int reorderLevel,
        int categoryId,
        bool isActive,
        string? categoryName = null)
    {
        return new Product(name, sku, unitPrice, costPrice, stockQuantity, reorderLevel, categoryId, isActive)
        {
            ProductId = productId,
            CategoryName = categoryName
        };
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity to reduce must be greater than zero.");
        }

        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(Name, quantity, StockQuantity);
        }

        StockQuantity -= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("Quantity to add must be greater than zero.");
        }

        StockQuantity += quantity;
    }

    public void AdjustStockTo(int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new BusinessRuleException("Stock quantity cannot be negative.");
        }

        StockQuantity = newQuantity;
    }

    public bool HasSufficientStock(int quantity) => quantity > 0 && StockQuantity >= quantity;

    public void UpdateDetails(
        string name,
        string sku,
        decimal unitPrice,
        decimal costPrice,
        int reorderLevel,
        int categoryId)
    {
        Name = name;
        Sku = sku;
        UnitPrice = unitPrice;
        CostPrice = costPrice;
        ReorderLevel = reorderLevel;
        CategoryId = categoryId;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void AssignIdentity(int productId) => ProductId = productId;

    public override string ToString() => $"{Sku} — {Name}";
}
