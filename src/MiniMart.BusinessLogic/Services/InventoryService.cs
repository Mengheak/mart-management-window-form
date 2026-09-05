using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;

namespace MiniMart.BusinessLogic.Services;

public class InventoryService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public InventoryService(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public Task<IReadOnlyList<Product>> GetProductsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        _productRepository.GetAllAsync(includeInactive, cancellationToken);

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        _productRepository.GetByIdAsync(productId, cancellationToken);

    public Task<IReadOnlyList<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default) =>
        _productRepository.SearchAsync(searchTerm ?? string.Empty, cancellationToken);

    public async Task<Product> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new BusinessRuleException("Please enter a product code.");
        }

        return await _productRepository.GetBySkuAsync(sku.Trim(), cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException($"No active product found with code '{sku.Trim()}'.");
    }

    public Task<IReadOnlyList<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default) =>
        _productRepository.GetLowStockAsync(cancellationToken);

    public async Task<Product> CreateProductAsync(
        string name,
        string sku,
        decimal unitPrice,
        decimal costPrice,
        int stockQuantity,
        int reorderLevel,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        var product = new Product(name, sku, unitPrice, costPrice, stockQuantity, reorderLevel, categoryId);

        await EnsureCategoryExistsAsync(categoryId, cancellationToken).ConfigureAwait(false);

        if (await _productRepository.SkuExistsAsync(product.Sku, 0, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"A product with SKU '{product.Sku}' already exists.");
        }

        var id = await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        product.AssignIdentity(id);
        return product;
    }

    public async Task UpdateProductAsync(
        int productId,
        string name,
        string sku,
        decimal unitPrice,
        decimal costPrice,
        int reorderLevel,
        int categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That product no longer exists.");

        product.UpdateDetails(name, sku, unitPrice, costPrice, reorderLevel, categoryId);

        if (isActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await EnsureCategoryExistsAsync(categoryId, cancellationToken).ConfigureAwait(false);

        if (await _productRepository.SkuExistsAsync(product.Sku, productId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"A product with SKU '{product.Sku}' already exists.");
        }

        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
    }

    public async Task AdjustStockAsync(
        int productId,
        int newQuantity,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That product no longer exists.");

        product.AdjustStockTo(newQuantity);
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That product no longer exists.");

        if (await _productRepository.HasSalesHistoryAsync(productId, cancellationToken).ConfigureAwait(false))
        {
            product.Deactivate();
            await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            return false;
        }

        await _productRepository.DeleteAsync(productId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository
            .GetByIdAsync(categoryId, cancellationToken)
            .ConfigureAwait(false);

        if (category is null)
        {
            throw new BusinessRuleException("Please choose a valid category for this product.");
        }
    }
}
