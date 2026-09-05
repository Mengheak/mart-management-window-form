using System.Data;
using Microsoft.Data.SqlClient;
using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Reporting;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.DataAccess.Infrastructure;

namespace MiniMart.DataAccess.Repositories;

public sealed class SaleRepository : SqlRepositoryBase, ISaleRepository
{
    public SaleRepository(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public async Task<Sale> SaveSaleAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        if (sale.Lines.Count == 0)
        {
            throw new BusinessRuleException("A sale must contain at least one line item.");
        }

        const string insertHeaderSql = @"
            INSERT INTO dbo.Sales (TotalAmount, AmountPaid, ChangeDue, CashierId)
            OUTPUT INSERTED.SaleId, INSERTED.SaleDate
            VALUES (@TotalAmount, @AmountPaid, @ChangeDue, @CashierId);";

        const string insertLineSql = @"
            INSERT INTO dbo.SaleDetails (SaleId, ProductId, Quantity, UnitPrice)
            VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice);";

        const string decrementStockSql = @"
            UPDATE dbo.Products
            SET StockQuantity = StockQuantity - @Quantity
            WHERE ProductId = @ProductId
              AND StockQuantity >= @Quantity;";

        await using var connection = await ConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Step 1 — begin the transaction. Nothing is visible to other sessions until commit.
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            // Step 2 — insert the header and retrieve the identity and timestamp the
            // database assigned, so the receipt shows the authoritative sale number.
            int saleId;
            DateTime saleDate;

            await using (var headerCommand = new SqlCommand(insertHeaderSql, connection, transaction))
            {
                var totalAmount = headerCommand.Parameters.Add("@TotalAmount", SqlDbType.Decimal);
                totalAmount.Precision = 10;
                totalAmount.Scale = 2;
                totalAmount.Value = sale.TotalAmount;

                var amountPaid = headerCommand.Parameters.Add("@AmountPaid", SqlDbType.Decimal);
                amountPaid.Precision = 10;
                amountPaid.Scale = 2;
                amountPaid.Value = sale.AmountPaid;

                var changeDue = headerCommand.Parameters.Add("@ChangeDue", SqlDbType.Decimal);
                changeDue.Precision = 10;
                changeDue.Scale = 2;
                changeDue.Value = sale.ChangeDue;

                headerCommand.Parameters.Add("@CashierId", SqlDbType.Int).Value = sale.CashierId;

                await using var reader = await headerCommand
                    .ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                    .ConfigureAwait(false);

                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new BusinessRuleException("The sale header could not be created.");
                }

                saleId = reader.GetInt32(reader.GetOrdinal("SaleId"));
                saleDate = reader.GetDateTime(reader.GetOrdinal("SaleDate"));
            }

            sale.AssignIdentity(saleId);
            sale.SetSaleDate(saleDate);

            foreach (var line in sale.Lines)
            {
                // Step 3 — insert the line item.
                await using (var lineCommand = new SqlCommand(insertLineSql, connection, transaction))
                {
                    lineCommand.Parameters.Add("@SaleId", SqlDbType.Int).Value = saleId;
                    lineCommand.Parameters.Add("@ProductId", SqlDbType.Int).Value = line.ProductId;
                    lineCommand.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;

                    var unitPrice = lineCommand.Parameters.Add("@UnitPrice", SqlDbType.Decimal);
                    unitPrice.Precision = 10;
                    unitPrice.Scale = 2;
                    unitPrice.Value = line.UnitPrice;

                    await lineCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                // Step 4 — reduce stock, but only if enough is still available right now.
                await using (var stockCommand = new SqlCommand(decrementStockSql, connection, transaction))
                {
                    stockCommand.Parameters.Add("@Quantity", SqlDbType.Int).Value = line.Quantity;
                    stockCommand.Parameters.Add("@ProductId", SqlDbType.Int).Value = line.ProductId;

                    var rowsAffected = await stockCommand
                        .ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (rowsAffected == 0)
                    {
                        // The guard rejected the update: stock ran out since the cart was built.
                        // Throwing here unwinds to the catch below, which rolls everything back.
                        throw new InsufficientStockException(
                            line.ProductName ?? $"Product #{line.ProductId}",
                            line.Quantity);
                    }
                }
            }

            // Step 5 — every line succeeded, so make the whole sale permanent at once.
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return sale;
        }
        catch
        {
            await SafeRollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
    }

    public Task<IReadOnlyList<Sale>> GetSalesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.SaleId, s.SaleDate, s.TotalAmount, s.AmountPaid, s.ChangeDue,
                   s.CashierId, u.Username AS CashierName
            FROM dbo.Sales AS s
            INNER JOIN dbo.Users AS u ON u.UserId = s.CashierId
            WHERE s.SaleDate >= @FromDate AND s.SaleDate <= @ToDate
            ORDER BY s.SaleDate DESC, s.SaleId DESC;";

        return QueryAsync(
            sql,
            parameters => BindDateRange(parameters, fromDate, toDate),
            MapSale,
            cancellationToken);
    }

    public async Task<Sale?> GetByIdAsync(int saleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT s.SaleId, s.SaleDate, s.TotalAmount, s.AmountPaid, s.ChangeDue,
                   s.CashierId, u.Username AS CashierName
            FROM dbo.Sales AS s
            INNER JOIN dbo.Users AS u ON u.UserId = s.CashierId
            WHERE s.SaleId = @SaleId;";

        var sale = await QuerySingleAsync(
            sql,
            parameters => parameters.Add("@SaleId", SqlDbType.Int).Value = saleId,
            MapSale,
            cancellationToken).ConfigureAwait(false);

        if (sale is null)
        {
            return null;
        }

        var lines = await GetLinesAsync(saleId, cancellationToken).ConfigureAwait(false);
        sale.AddLines(lines);
        return sale;
    }

    public Task<IReadOnlyList<SaleDetail>> GetLinesAsync(
        int saleId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT d.SaleDetailId, d.SaleId, d.ProductId, d.Quantity, d.UnitPrice,
                   p.Name AS ProductName
            FROM dbo.SaleDetails AS d
            INNER JOIN dbo.Products AS p ON p.ProductId = d.ProductId
            WHERE d.SaleId = @SaleId
            ORDER BY d.SaleDetailId;";

        return QueryAsync(
            sql,
            parameters => parameters.Add("@SaleId", SqlDbType.Int).Value = saleId,
            MapSaleDetail,
            cancellationToken);
    }

    public Task<IReadOnlyList<DailySalesTotal>> GetDailyTotalsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        // The unit count is gathered in a correlated subquery rather than by joining
        // SaleDetails directly: a join would repeat each header once per line item and
        // inflate both SaleCount and TotalRevenue.
        const string sql = @"
            SELECT CAST(s.SaleDate AS DATE)      AS SaleDay,
                   COUNT(*)                      AS SaleCount,
                   ISNULL(SUM(lines.Units), 0)   AS UnitsSold,
                   ISNULL(SUM(s.TotalAmount), 0) AS TotalRevenue
            FROM dbo.Sales AS s
            OUTER APPLY (
                SELECT SUM(d.Quantity) AS Units
                FROM dbo.SaleDetails AS d
                WHERE d.SaleId = s.SaleId
            ) AS lines
            WHERE s.SaleDate >= @FromDate AND s.SaleDate <= @ToDate
            GROUP BY CAST(s.SaleDate AS DATE)
            ORDER BY SaleDay DESC;";

        return QueryAsync(
            sql,
            parameters => BindDateRange(parameters, fromDate, toDate),
            MapDailyTotal,
            cancellationToken);
    }

    public Task<IReadOnlyList<BestSellingProduct>> GetBestSellingProductsAsync(
        DateTime fromDate,
        DateTime toDate,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@TopCount)
                   p.ProductId,
                   p.SKU,
                   p.Name                            AS ProductName,
                   SUM(d.Quantity)                   AS UnitsSold,
                   SUM(d.Quantity * d.UnitPrice)     AS Revenue
            FROM dbo.SaleDetails AS d
            INNER JOIN dbo.Sales    AS s ON s.SaleId = d.SaleId
            INNER JOIN dbo.Products AS p ON p.ProductId = d.ProductId
            WHERE s.SaleDate >= @FromDate AND s.SaleDate <= @ToDate
            GROUP BY p.ProductId, p.SKU, p.Name
            ORDER BY UnitsSold DESC, Revenue DESC;";

        return QueryAsync(
            sql,
            parameters =>
            {
                parameters.Add("@TopCount", SqlDbType.Int).Value = topCount;
                BindDateRange(parameters, fromDate, toDate);
            },
            MapBestSeller,
            cancellationToken);
    }

    private static void BindDateRange(SqlParameterCollection parameters, DateTime fromDate, DateTime toDate)
    {
        parameters.Add("@FromDate", SqlDbType.DateTime2).Value = fromDate;
        parameters.Add("@ToDate", SqlDbType.DateTime2).Value = toDate;
    }

    private static async Task SafeRollbackAsync(SqlTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The connection may already be broken, in which case the server has aborted the
            // transaction for us. Either way nothing was committed, which is the guarantee
            // that matters here.
        }
    }

    private static Sale MapSale(SqlDataReader reader) =>
        Sale.FromDatabase(
            reader.GetInt32(reader.GetOrdinal("SaleId")),
            reader.GetDateTime(reader.GetOrdinal("SaleDate")),
            reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
            reader.GetDecimal(reader.GetOrdinal("AmountPaid")),
            reader.GetDecimal(reader.GetOrdinal("ChangeDue")),
            reader.GetInt32(reader.GetOrdinal("CashierId")),
            ReadNullableString(reader, "CashierName"));

    private static SaleDetail MapSaleDetail(SqlDataReader reader) =>
        SaleDetail.FromDatabase(
            reader.GetInt32(reader.GetOrdinal("SaleDetailId")),
            reader.GetInt32(reader.GetOrdinal("SaleId")),
            reader.GetInt32(reader.GetOrdinal("ProductId")),
            reader.GetInt32(reader.GetOrdinal("Quantity")),
            reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
            ReadNullableString(reader, "ProductName"));

    private static DailySalesTotal MapDailyTotal(SqlDataReader reader) =>
        new(
            reader.GetDateTime(reader.GetOrdinal("SaleDay")),
            reader.GetInt32(reader.GetOrdinal("SaleCount")),
            reader.GetInt32(reader.GetOrdinal("UnitsSold")),
            reader.GetDecimal(reader.GetOrdinal("TotalRevenue")));

    private static BestSellingProduct MapBestSeller(SqlDataReader reader) =>
        new(
            reader.GetInt32(reader.GetOrdinal("ProductId")),
            reader.GetString(reader.GetOrdinal("SKU")),
            reader.GetString(reader.GetOrdinal("ProductName")),
            reader.GetInt32(reader.GetOrdinal("UnitsSold")),
            reader.GetDecimal(reader.GetOrdinal("Revenue")));
}
