using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.DTOs;

public sealed record OrderResponse(
    Guid Id,
    string CustomerEmail,
    IReadOnlyCollection<OrderItemResponse> Items,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt);
