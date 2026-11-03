namespace OrderManagement.Application.Exceptions;

public sealed class OrderNotFoundException : Exception
{
    public OrderNotFoundException(Guid orderId)
        : base($"Orden '{orderId}' no fue encontrada.")
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}
