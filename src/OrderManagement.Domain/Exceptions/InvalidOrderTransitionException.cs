using OrderManagement.Domain.Enums;

namespace OrderManagement.Domain.Exceptions;

public sealed class InvalidOrderTransitionException : Exception
{
    public InvalidOrderTransitionException(OrderStatus currentStatus, OrderStatus requestedStatus)
        : base($"No se puede transicionar una orden de {currentStatus} a {requestedStatus}.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public OrderStatus CurrentStatus { get; }

    public OrderStatus RequestedStatus { get; }
}
