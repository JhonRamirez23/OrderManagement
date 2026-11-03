using System.Collections.Concurrent;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Infrastructure.Observers;

namespace OrderManagement.Infrastructure.Inventory;

public sealed class InMemoryInventoryRepository : IInventoryRepository, IInventorySubject
{
    private readonly IProductRepository productRepository;
    private readonly SemaphoreSlim inventoryLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<InventoryReservationItem>> reservations = new();
    private readonly ConcurrentDictionary<IInventoryObserver, byte> observers = new();

    public InMemoryInventoryRepository(IProductRepository productRepository)
    {
        this.productRepository = productRepository;
    }

    public async Task<InventoryReservation> ReserveAsync(
        IReadOnlyCollection<InventoryReservationItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        var requestedItems = NormalizeItems(items);

        await inventoryLock.WaitAsync(cancellationToken);
        InventoryReservation reservation;
        try
        {
            var products = await productRepository.GetByIdsAsync(
                requestedItems.Select(item => item.ProductId).ToArray(),
                cancellationToken);
            var productsById = products.ToDictionary(product => product.Id);

            foreach (var item in requestedItems)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    throw new ProductNotFoundException(item.ProductId);
                }

                if (product.Stock < item.Quantity)
                {
                    throw new InsufficientInventoryException(
                        $"Insufficient inventory for product '{item.ProductId}'.");
                }
            }

            foreach (var item in requestedItems)
            {
                productsById[item.ProductId].DecreaseStock(item.Quantity);
            }

            var reservationId = Guid.NewGuid();
            reservations[reservationId] = requestedItems;
            reservation = new InventoryReservation(reservationId);
        }
        finally
        {
            inventoryLock.Release();
        }

        await NotifyObserversAsync(
            new InventoryChange(
                InventoryChangeType.Reserved,
                reservation.ReservationId,
                ToChangeItems(requestedItems)),
            cancellationToken);
        return reservation;
    }

    public async Task ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            return;
        }

        IReadOnlyCollection<InventoryReservationItem> reservedItems;
        await inventoryLock.WaitAsync(cancellationToken);
        try
        {
            if (!reservations.TryRemove(reservationId, out reservedItems!))
            {
                return;
            }

            var products = await productRepository.GetByIdsAsync(
                reservedItems.Select(item => item.ProductId).ToArray(),
                cancellationToken);
            var productsById = products.ToDictionary(product => product.Id);

            foreach (var item in reservedItems)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    throw new ProductNotFoundException(item.ProductId);
                }

                product.IncreaseStock(item.Quantity);
            }
        }
        finally
        {
            inventoryLock.Release();
        }

        await NotifyObserversAsync(
            new InventoryChange(
                InventoryChangeType.Released,
                reservationId,
                ToChangeItems(reservedItems)),
            cancellationToken);
    }

    public void Subscribe(IInventoryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        observers.TryAdd(observer, 0);
    }

    public void Unsubscribe(IInventoryObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        observers.TryRemove(observer, out _);
    }

    private async Task NotifyObserversAsync(
        InventoryChange change,
        CancellationToken cancellationToken)
    {
        foreach (var observer in observers.Keys)
        {
            try
            {
                await observer.OnInventoryChangedAsync(change, cancellationToken);
            }
            catch (Exception)
            {
            }
        }
    }

    private static IReadOnlyCollection<InventoryReservationItem> NormalizeItems(
        IReadOnlyCollection<InventoryReservationItem> items)
    {
        if (items.Count == 0 || items.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0))
        {
            throw new ApplicationValidationException("Inventory reservation items are invalid.");
        }

        return items
            .GroupBy(item => item.ProductId)
            .Select(group => new InventoryReservationItem(
                group.Key,
                group.Sum(item => item.Quantity)))
            .ToArray();
    }

    private static IReadOnlyCollection<InventoryChangeItem> ToChangeItems(
        IReadOnlyCollection<InventoryReservationItem> items)
    {
        return items
            .Select(item => new InventoryChangeItem(item.ProductId, item.Quantity))
            .ToArray();
    }
}
