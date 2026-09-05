using System.Data;
using Microsoft.Data.SqlClient;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.DataAccess.Infrastructure;

namespace MiniMart.DataAccess.Repositories;

public sealed class ProductRepository : SqlRepositoryBase, IProductRepository
{
    private const string SelectColumns = @"
        p.ProductId, p.Name, p.SKU, p.UnitPrice, p.CostPrice,
        p.StockQuantity, p.ReorderLevel, p.CategoryId, p.IsActive,
        c.Name AS CategoryName";

    public ProductRepository(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<Product>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Products AS p
            INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
            WHERE (@IncludeInactive = 1 OR p.IsActive = 1)
            ORDER BY p.Name;";

        return QueryAsync(
            sql,
            parameters => parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive,
            Map,
            cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Products AS p
            INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
            WHERE p.ProductId = @ProductId;";

        return QuerySingleAsync(
            sql,
            parameters => parameters.Add("@ProductId", SqlDbType.Int).Value = productId,
            Map,
            cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Products AS p
            INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
            WHERE p.SKU = @SKU AND p.IsActive = 1;";

        return QuerySingleAsync(
            sql,
            parameters => parameters.Add("@SKU", SqlDbType.NVarChar, Product.SkuMaxLength).Value = sku ?? string.Empty,
            Map,
            cancellationToken);
    }

    public Task<IReadOnlyList<Product>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Products AS p
            INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
            WHERE p.IsActive = 1
              AND (@Pattern = N'%%'
                   OR p.Name LIKE @Pattern ESCAPE N'\'
                   OR p.SKU  LIKE @Pattern ESCAPE N'\')
            ORDER BY p.Name;";

        var pattern = $"%{EscapeLikePattern(searchTerm ?? string.Empty)}%";

        return QueryAsync(
            sql,
            parameters => parameters.Add("@Pattern", SqlDbType.NVarChar, 200).Value = pattern,
            Map,
            cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Products AS p
            INNER JOIN dbo.Categories AS c ON c.CategoryId = p.CategoryId
            WHERE p.IsActive = 1
              AND p.StockQuantity <= p.ReorderLevel
            ORDER BY (p.StockQuantity - p.ReorderLevel), p.Name;";

        return QueryAsync(sql, _ => { }, Map, cancellationToken);
    }

    public async Task<int> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        const string sql = @"
            INSERT INTO dbo.Products
                (Name, SKU, UnitPrice, CostPrice, StockQuantity, ReorderLevel, CategoryId, IsActive)
            OUTPUT INSERTED.ProductId
            VALUES
                (@Name, @SKU, @UnitPrice, @CostPrice, @StockQuantity, @ReorderLevel, @CategoryId, @IsActive);";

        return await ExecuteScalarAsync<int>(
            sql,
            parameters => BindProduct(parameters, product),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<int> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        const string sql = @"
            UPDATE dbo.Products
            SET Name          = @Name,
                SKU           = @SKU,
                UnitPrice     = @UnitPrice,
                CostPrice     = @CostPrice,
                StockQuantity = @StockQuantity,
                ReorderLevel  = @ReorderLevel,
                CategoryId    = @CategoryId,
                IsActive      = @IsActive
            WHERE ProductId = @ProductId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters =>
            {
                BindProduct(parameters, product);
                parameters.Add("@ProductId", SqlDbType.Int).Value = product.ProductId;
            },
            cancellationToken);
    }

    public Task<int> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM dbo.Products
            WHERE ProductId = @ProductId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters => parameters.Add("@ProductId", SqlDbType.Int).Value = productId,
            cancellationToken);
    }

    public async Task<bool> SkuExistsAsync(
        string sku,
        int excludeProductId = 0,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Products
            WHERE SKU = @SKU
              AND ProductId <> @ExcludeProductId;";

        var count = await ExecuteScalarAsync<int>(
            sql,
            parameters =>
            {
                parameters.Add("@SKU", SqlDbType.NVarChar, Product.SkuMaxLength).Value = sku ?? string.Empty;
                parameters.Add("@ExcludeProductId", SqlDbType.Int).Value = excludeProductId;
            },
            cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    public async Task<bool> HasSalesHistoryAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.SaleDetails
            WHERE ProductId = @ProductId;";

        var count = await ExecuteScalarAsync<int>(
            sql,
            parameters => parameters.Add("@ProductId", SqlDbType.Int).Value = productId,
            cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    private static void BindProduct(SqlParameterCollection parameters, Product product)
    {
        parameters.Add("@Name", SqlDbType.NVarChar, Product.NameMaxLength).Value = product.Name;
        parameters.Add("@SKU", SqlDbType.NVarChar, Product.SkuMaxLength).Value = product.Sku;
        parameters.Add("@UnitPrice", SqlDbType.Decimal).Value = product.UnitPrice;
        parameters.Add("@CostPrice", SqlDbType.Decimal).Value = product.CostPrice;
        parameters.Add("@StockQuantity", SqlDbType.Int).Value = product.StockQuantity;
        parameters.Add("@ReorderLevel", SqlDbType.Int).Value = product.ReorderLevel;
        parameters.Add("@CategoryId", SqlDbType.Int).Value = product.CategoryId;
        parameters.Add("@IsActive", SqlDbType.Bit).Value = product.IsActive;

        // DECIMAL(10,2) in the schema; setting precision explicitly stops the provider from
        // inferring a scale that would silently truncate the value.
        parameters["@UnitPrice"].Precision = 10;
        parameters["@UnitPrice"].Scale = 2;
        parameters["@CostPrice"].Precision = 10;
        parameters["@CostPrice"].Scale = 2;
    }

    private static string EscapeLikePattern(string term) =>
        term.Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_")
            .Replace("[", @"\[");

    private static Product Map(SqlDataReader reader) =>
        Product.FromDatabase(
            reader.GetInt32(reader.GetOrdinal("ProductId")),
            reader.GetString(reader.GetOrdinal("Name")),
            reader.GetString(reader.GetOrdinal("SKU")),
            reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
            reader.GetDecimal(reader.GetOrdinal("CostPrice")),
            reader.GetInt32(reader.GetOrdinal("StockQuantity")),
            reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
            reader.GetInt32(reader.GetOrdinal("CategoryId")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            ReadNullableString(reader, "CategoryName"));
}
