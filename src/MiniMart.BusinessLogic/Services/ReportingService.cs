using MiniMart.BusinessLogic.Exceptions;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Reporting;
using MiniMart.BusinessLogic.Repositories;

namespace MiniMart.BusinessLogic.Services;

public class ReportingService
{
    private readonly ISaleRepository _saleRepository;

    public ReportingService(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository ?? throw new ArgumentNullException(nameof(saleRepository));
    }

    public Task<IReadOnlyList<Sale>> GetSalesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormaliseRange(fromDate, toDate);
        return _saleRepository.GetSalesAsync(from, to, cancellationToken);
    }

    public async Task<Sale> GetSaleAsync(int saleId, CancellationToken cancellationToken = default)
    {
        return await _saleRepository.GetByIdAsync(saleId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException($"Sale #{saleId} could not be found.");
    }

    public Task<IReadOnlyList<SaleDetail>> GetSaleLinesAsync(
        int saleId,
        CancellationToken cancellationToken = default) =>
        _saleRepository.GetLinesAsync(saleId, cancellationToken);

    public Task<IReadOnlyList<DailySalesTotal>> GetDailyTotalsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = NormaliseRange(fromDate, toDate);
        return _saleRepository.GetDailyTotalsAsync(from, to, cancellationToken);
    }

    public Task<IReadOnlyList<BestSellingProduct>> GetBestSellingProductsAsync(
        DateTime fromDate,
        DateTime toDate,
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        if (topCount <= 0)
        {
            throw new BusinessRuleException("The number of products to list must be greater than zero.");
        }

        var (from, to) = NormaliseRange(fromDate, toDate);
        return _saleRepository.GetBestSellingProductsAsync(from, to, topCount, cancellationToken);
    }

    private static (DateTime From, DateTime To) NormaliseRange(DateTime fromDate, DateTime toDate)
    {
        if (fromDate.Date > toDate.Date)
        {
            throw new BusinessRuleException("The start date cannot be later than the end date.");
        }

        return (fromDate.Date, toDate.Date.AddDays(1).AddSeconds(-1));
    }
}
