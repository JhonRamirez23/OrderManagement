namespace OrderManagement.Application.Exceptions;

public sealed class ProductNotFoundException : Exception
{
    public ProductNotFoundException(Guid productId)
        : base($"Producto '{productId}' no fue encontrado.")
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
