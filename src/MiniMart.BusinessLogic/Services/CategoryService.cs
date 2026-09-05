using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;

namespace MiniMart.BusinessLogic.Services;

public class CategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _categoryRepository.GetAllAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default) =>
        _categoryRepository.GetByIdAsync(categoryId, cancellationToken);

    public async Task<Category> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var category = new Category(name);

        if (await _categoryRepository.NameExistsAsync(category.Name, 0, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"A category named '{category.Name}' already exists.");
        }

        var id = await _categoryRepository.AddAsync(category, cancellationToken).ConfigureAwait(false);
        category.AssignIdentity(id);
        return category;
    }

    public async Task UpdateAsync(
        int categoryId,
        string newName,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("That category no longer exists.");

        category.Rename(newName);

        if (await _categoryRepository.NameExistsAsync(category.Name, categoryId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"A category named '{category.Name}' already exists.");
        }

        await _categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var productCount = await _categoryRepository
            .CountProductsAsync(categoryId, cancellationToken)
            .ConfigureAwait(false);

        if (productCount > 0)
        {
            throw new BusinessRuleException(
                $"This category cannot be deleted because {productCount} product(s) still belong to it. " +
                "Reassign or remove those products first.");
        }

        await _categoryRepository.DeleteAsync(categoryId, cancellationToken).ConfigureAwait(false);
    }
}
