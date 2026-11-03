namespace OrderManagement.Application.DTOs;

public sealed record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    int Stock);
