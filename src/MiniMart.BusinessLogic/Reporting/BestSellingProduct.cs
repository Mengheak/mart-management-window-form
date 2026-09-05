namespace MiniMart.BusinessLogic.Reporting;

public sealed record BestSellingProduct(
    int ProductId,
    string Sku,
    string ProductName,
    int UnitsSold,
    decimal Revenue);
