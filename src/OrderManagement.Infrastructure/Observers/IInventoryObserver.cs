namespace OrderManagement.Infrastructure.Observers;

public interface IInventoryObserver
{
    Task OnInventoryChangedAsync(
        InventoryChange change,
        CancellationToken cancellationToken = default);
}
