using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyCollection<Product>> GetByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
