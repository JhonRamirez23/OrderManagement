using System.Collections.Concurrent;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> products;

    public InMemoryProductRepository(IEnumerable<Product>? initialProducts = null)
    {
        products = new ConcurrentDictionary<Guid, Product>(
            (initialProducts ?? CreateDefaultProducts()).ToDictionary(product => product.Id));
    }

    public Task<IReadOnlyCollection<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = productIds
            .Distinct()
            .Where(products.ContainsKey)
            .Select(productId => products[productId])
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<Product>>(result);
    }

    public Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<Product>>(products.Values.ToArray());
    }

    private static IEnumerable<Product> CreateDefaultProducts()
    {
        yield return new Product(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Keyboard",
            25.50m,
            10);
        yield return new Product(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Mouse",
            12.75m,
            20);
        yield return new Product(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "Monitor",
            199.99m,
            5);
    }
}
