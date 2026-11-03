using OrderManagement.Application.DTOs;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services;

public sealed class GetOrderUseCase
{
    private readonly IOrderRepository orderRepository;

    public GetOrderUseCase(IOrderRepository orderRepository)
    {
        this.orderRepository = orderRepository;
    }

    public async Task<OrderResponse> ExecuteAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ApplicationValidationException("Order id is required.");
        }

        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new OrderNotFoundException(orderId);
        }

        return Map(order);
    }

    private static OrderResponse Map(Order order)
    {
        var items = order.Items
            .Select(item => new OrderItemResponse(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.Subtotal))
            .ToArray();

        return new OrderResponse(
            order.Id,
            order.CustomerEmail,
            items,
            order.TotalAmount,
            order.Status,
            order.CreatedAt);
    }
}
