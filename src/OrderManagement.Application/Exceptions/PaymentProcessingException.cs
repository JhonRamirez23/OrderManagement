namespace OrderManagement.Application.Exceptions;

public sealed class PaymentProcessingException : Exception
{
    public PaymentProcessingException(Guid orderId, string? message = null)
        : base(message ?? $"El procesamiento de pagos para la orden '{orderId}' tiene un resultado desconocido.")
    {
        OrderId = orderId;
    }

    public Guid OrderId { get; }
}
