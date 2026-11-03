namespace OrderManagement.Application.DTOs;

public sealed class CreateOrderRequest
{
    public string CustomerEmail { get; init; } = string.Empty;

    public IReadOnlyCollection<CreateOrderItemRequest> Items { get; init; } = [];
}
