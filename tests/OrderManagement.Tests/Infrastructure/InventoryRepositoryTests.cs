using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Inventory;
using OrderManagement.Infrastructure.Observers;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.Infrastructure;

public sealed class InventoryRepositoryTests
{
    [Fact]
    public async Task Reserve_IsAtomicWhenOneProductHasInsufficientStock()
    {
        var firstProduct = new Product(Guid.NewGuid(), "Teclado", 25m, 1);
        var secondProduct = new Product(Guid.NewGuid(), "Ratón", 10m, 0);
        var products = new InMemoryProductRepository([firstProduct, secondProduct]);
        var inventory = new InMemoryInventoryRepository(products);

        await Assert.ThrowsAsync<InsufficientInventoryException>(() => inventory.ReserveAsync(
        [
            new InventoryReservationItem(firstProduct.Id, 1),
            new InventoryReservationItem(secondProduct.Id, 1)
        ]));

        Assert.Equal(1, firstProduct.Stock);
        Assert.Equal(0, secondProduct.Stock);
    }

    [Fact]
    public async Task Release_IsIdempotent()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", 25m, 1);
        var inventory = new InMemoryInventoryRepository(new InMemoryProductRepository([product]));
        var reservation = await inventory.ReserveAsync(
            [new InventoryReservationItem(product.Id, 1)]);

        await inventory.ReleaseAsync(reservation.ReservationId);
        await inventory.ReleaseAsync(reservation.ReservationId);

        Assert.Equal(1, product.Stock);
    }

    [Fact]
    public async Task ReserveAndRelease_NotifyObserverAfterSuccessfulOperations()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", 25m, 1);
        var inventory = new InMemoryInventoryRepository(new InMemoryProductRepository([product]));
        var observer = new InMemoryInventoryObserver();
        ((IInventorySubject)inventory).Subscribe(observer);

        var reservation = await inventory.ReserveAsync(
            [new InventoryReservationItem(product.Id, 1)]);
        await inventory.ReleaseAsync(reservation.ReservationId);

        Assert.Equal(2, observer.Changes.Count);
        Assert.Equal(InventoryChangeType.Reserved, observer.Changes.ElementAt(0).Type);
        Assert.Equal(InventoryChangeType.Released, observer.Changes.ElementAt(1).Type);
    }

    [Fact]
    public async Task ConcurrentReservations_DoNotOversellInventory()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", 25m, 1);
        var inventory = new InMemoryInventoryRepository(new InMemoryProductRepository([product]));
        var requests = Enumerable.Range(0, 20)
            .Select(_ => TryReserveAsync(inventory, product.Id))
            .ToArray();

        var results = await Task.WhenAll(requests);

        Assert.Equal(1, results.Count(result => result));
        Assert.Equal(0, product.Stock);
        Assert.True(product.Stock >= 0);
    }

    private static async Task<bool> TryReserveAsync(
        IInventoryRepository inventory,
        Guid productId)
    {
        try
        {
            await inventory.ReserveAsync([new InventoryReservationItem(productId, 1)]);
            return true;
        }
        catch (InsufficientInventoryException)
        {
            return false;
        }
    }
}
