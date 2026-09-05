using MiniMart.BusinessLogic.Models;

namespace MiniMart.BusinessLogic.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<int> AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int excludeCategoryId = 0,
        CancellationToken cancellationToken = default);

    Task<int> CountProductsAsync(int categoryId, CancellationToken cancellationToken = default);
}
