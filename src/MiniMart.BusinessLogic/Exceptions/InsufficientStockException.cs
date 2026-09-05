namespace MiniMart.BusinessLogic.Exceptions;

public class InsufficientStockException : BusinessRuleException
{
    public string ProductName { get; }

    public int Requested { get; }

    public int Available { get; }

    public InsufficientStockException(string productName, int requested, int available)
        : base($"Insufficient stock for '{productName}'. Requested {requested}, but only {available} available.")
    {
        ProductName = productName;
        Requested = requested;
        Available = available;
    }

    public InsufficientStockException(string productName, int requested)
        : base($"Insufficient stock for '{productName}'. Requested {requested}, but stock ran out before the sale could be completed.")
    {
        ProductName = productName;
        Requested = requested;
        Available = -1;
    }
}
