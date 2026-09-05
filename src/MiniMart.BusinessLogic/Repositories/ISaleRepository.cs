using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Reporting;

namespace MiniMart.BusinessLogic.Repositories;

public interface ISaleRepository
{
    Task<Sale> SaveSaleAsync(Sale sale, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetSalesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<Sale?> GetByIdAsync(int saleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SaleDetail>> GetLinesAsync(
        int saleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailySalesTotal>> GetDailyTotalsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BestSellingProduct>> GetBestSellingProductsAsync(
        DateTime fromDate,
        DateTime toDate,
        int topCount = 10,
        CancellationToken cancellationToken = default);
}
