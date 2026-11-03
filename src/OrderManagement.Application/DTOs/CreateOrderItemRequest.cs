namespace OrderManagement.Application.DTOs;

public sealed class CreateOrderItemRequest
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }
}
