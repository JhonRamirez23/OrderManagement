namespace OrderManagement.Application.Exceptions;

public sealed class InsufficientInventoryException : Exception
{
    public InsufficientInventoryException(string message = "Insufficient inventory.")
        : base(message)
    {
    }
}
