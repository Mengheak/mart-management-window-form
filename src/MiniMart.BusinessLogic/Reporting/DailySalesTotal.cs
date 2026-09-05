namespace MiniMart.BusinessLogic.Reporting;

public sealed record DailySalesTotal(
    DateTime Date,
    int SaleCount,
    int UnitsSold,
    decimal TotalRevenue)
{
    public decimal AverageSaleValue =>
        SaleCount == 0 ? 0m : decimal.Round(TotalRevenue / SaleCount, 2, MidpointRounding.AwayFromZero);
}
