using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.Entities;

public sealed class Order
{
    public Order(Guid id, string customerEmail, IEnumerable<OrderItem> items, DateTimeOffset? createdAt = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("La orden es requerida.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new ArgumentException("El correo del cliente es requerido.", nameof(customerEmail));
        }

        ArgumentNullException.ThrowIfNull(items);

        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            throw new ArgumentException("Una orden debe contener al menos un artículo.", nameof(items));
        }

        Id = id;
        CustomerEmail = customerEmail.Trim();
        Items = itemList.AsReadOnly();
        TotalAmount = itemList.Sum(item => item.Subtotal);
        Status = OrderStatus.PendingPayment;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public string CustomerEmail { get; }

    public IReadOnlyCollection<OrderItem> Items { get; }

    public decimal TotalAmount { get; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void TransitionTo(OrderStatus newStatus)
    {
        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOrderTransitionException(Status, newStatus);
        }

        Status = newStatus;
    }

    private static bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return currentStatus == OrderStatus.PendingPayment
            && (newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.PaymentFailed);
    }
}
