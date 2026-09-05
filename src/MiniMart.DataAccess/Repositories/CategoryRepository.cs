using System.Data;
using Microsoft.Data.SqlClient;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.DataAccess.Infrastructure;

namespace MiniMart.DataAccess.Repositories;

public sealed class CategoryRepository : SqlRepositoryBase, ICategoryRepository
{
    public CategoryRepository(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CategoryId, Name
            FROM dbo.Categories
            ORDER BY Name;";

        return QueryAsync(sql, _ => { }, Map, cancellationToken);
    }

    public Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CategoryId, Name
            FROM dbo.Categories
            WHERE CategoryId = @CategoryId;";

        return QuerySingleAsync(
            sql,
            parameters => parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId,
            Map,
            cancellationToken);
    }

    public async Task<int> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        const string sql = @"
            INSERT INTO dbo.Categories (Name)
            OUTPUT INSERTED.CategoryId
            VALUES (@Name);";

        return await ExecuteScalarAsync<int>(
            sql,
            parameters => parameters.Add("@Name", SqlDbType.NVarChar, Category.NameMaxLength).Value = category.Name,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<int> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        const string sql = @"
            UPDATE dbo.Categories
            SET Name = @Name
            WHERE CategoryId = @CategoryId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters =>
            {
                parameters.Add("@Name", SqlDbType.NVarChar, Category.NameMaxLength).Value = category.Name;
                parameters.Add("@CategoryId", SqlDbType.Int).Value = category.CategoryId;
            },
            cancellationToken);
    }

    public Task<int> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM dbo.Categories
            WHERE CategoryId = @CategoryId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters => parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId,
            cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int excludeCategoryId = 0,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Categories
            WHERE Name = @Name
              AND CategoryId <> @ExcludeCategoryId;";

        var count = await ExecuteScalarAsync<int>(
            sql,
            parameters =>
            {
                parameters.Add("@Name", SqlDbType.NVarChar, Category.NameMaxLength).Value = name ?? string.Empty;
                parameters.Add("@ExcludeCategoryId", SqlDbType.Int).Value = excludeCategoryId;
            },
            cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    public Task<int> CountProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Products
            WHERE CategoryId = @CategoryId;";

        return ExecuteScalarAsync<int>(
            sql,
            parameters => parameters.Add("@CategoryId", SqlDbType.Int).Value = categoryId,
            cancellationToken);
    }

    private static Category Map(SqlDataReader reader) =>
        Category.FromDatabase(
            reader.GetInt32(reader.GetOrdinal("CategoryId")),
            reader.GetString(reader.GetOrdinal("Name")));
}
