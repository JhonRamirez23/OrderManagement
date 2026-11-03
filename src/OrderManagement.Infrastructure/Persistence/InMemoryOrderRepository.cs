using System.Collections.Concurrent;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> orders = new();

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        if (!orders.TryAdd(order.Id, order))
        {
            throw new InvalidOperationException($"Order '{order.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();
        orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
