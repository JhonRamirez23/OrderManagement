namespace OrderManagement.Infrastructure.Observers;

public interface IInventorySubject
{
    void Subscribe(IInventoryObserver observer);

    void Unsubscribe(IInventoryObserver observer);
}

public sealed record InventoryChange(
    InventoryChangeType Type,
    Guid ReservationId,
    IReadOnlyCollection<InventoryChangeItem> Items);

public sealed record InventoryChangeItem(Guid ProductId, int Quantity);

public enum InventoryChangeType
{
    Reserved,
    Released
}
