using MiniMart.BusinessLogic.Models;

namespace MiniMart.BusinessLogic.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default);

    Task<int> AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int productId, CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(
        string sku,
        int excludeProductId = 0,
        CancellationToken cancellationToken = default);

    Task<bool> HasSalesHistoryAsync(int productId, CancellationToken cancellationToken = default);
}
