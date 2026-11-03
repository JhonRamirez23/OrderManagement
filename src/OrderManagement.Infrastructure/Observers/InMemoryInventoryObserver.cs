using System.Collections.Concurrent;

namespace OrderManagement.Infrastructure.Observers;

public sealed class InMemoryInventoryObserver : IInventoryObserver
{
    private readonly ConcurrentQueue<InventoryChange> changes = new();

    public IReadOnlyCollection<InventoryChange> Changes => changes.ToArray();

    public Task OnInventoryChangedAsync(
        InventoryChange change,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        changes.Enqueue(change);
        return Task.CompletedTask;
    }
}
