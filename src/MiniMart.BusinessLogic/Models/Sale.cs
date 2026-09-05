using MiniMart.BusinessLogic.Exceptions;

namespace MiniMart.BusinessLogic.Models;

public class Sale
{
    private readonly List<SaleDetail> _lines = new();

    public int SaleId { get; private set; }

    public DateTime SaleDate { get; private set; } = DateTime.Now;

    public decimal TotalAmount { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal ChangeDue { get; private set; }

    public int CashierId { get; private set; }

    public string? CashierName { get; private set; }

    public IReadOnlyList<SaleDetail> Lines => _lines;

    public decimal Subtotal =>
        decimal.Round(_lines.Sum(line => line.LineTotal), 2, MidpointRounding.AwayFromZero);

    public decimal DiscountAmount => decimal.Round(Subtotal - TotalAmount, 2, MidpointRounding.AwayFromZero);

    public int TotalUnits => _lines.Sum(line => line.Quantity);

    public Sale(int cashierId, decimal totalAmount, decimal amountPaid)
    {
        if (cashierId <= 0)
        {
            throw new BusinessRuleException("A sale must record the cashier who processed it.");
        }

        if (totalAmount < 0m)
        {
            throw new BusinessRuleException("Sale total cannot be negative.");
        }

        if (amountPaid < 0m)
        {
            throw new BusinessRuleException("Amount paid cannot be negative.");
        }

        var total = decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero);
        var paid = decimal.Round(amountPaid, 2, MidpointRounding.AwayFromZero);

        if (paid < total)
        {
            throw new BusinessRuleException(
                $"Amount paid ({paid:N2}) is less than the sale total ({total:N2}).");
        }

        CashierId = cashierId;
        TotalAmount = total;
        AmountPaid = paid;
        ChangeDue = decimal.Round(paid - total, 2, MidpointRounding.AwayFromZero);
    }

    public static Sale FromDatabase(
        int saleId,
        DateTime saleDate,
        decimal totalAmount,
        decimal amountPaid,
        decimal changeDue,
        int cashierId,
        string? cashierName = null)
    {
        // Bypasses the tender validation in the public constructor: historical rows are facts
        // already accepted by the database and must round-trip exactly as stored.
        return new Sale(cashierId, 0m, 0m)
        {
            SaleId = saleId,
            SaleDate = saleDate,
            TotalAmount = totalAmount,
            AmountPaid = amountPaid,
            ChangeDue = changeDue,
            CashierName = cashierName
        };
    }

    public void AddLine(SaleDetail line)
    {
        ArgumentNullException.ThrowIfNull(line);
        _lines.Add(line);
    }

    public void AddLines(IEnumerable<SaleDetail> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        foreach (var line in lines)
        {
            AddLine(line);
        }
    }

    public void AssignIdentity(int saleId)
    {
        SaleId = saleId;

        foreach (var line in _lines)
        {
            line.AssignSaleId(saleId);
        }
    }

    public void SetSaleDate(DateTime saleDate) => SaleDate = saleDate;
}
