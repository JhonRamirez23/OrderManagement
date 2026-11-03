namespace OrderManagement.Domain.Entities;

public sealed class OrderItem
{
    public OrderItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("El producto es requerido.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("El nombre del producto es requerido.", nameof(productName));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "El precio unitario no puede ser negativo.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor que cero.");
        }

        ProductId = productId;
        ProductName = productName.Trim();
        UnitPrice = unitPrice;
        Quantity = quantity;
        Subtotal = unitPrice * quantity;
    }

    public Guid ProductId { get; }

    public string ProductName { get; }

    public decimal UnitPrice { get; }

    public int Quantity { get; }

    public decimal Subtotal { get; }
}
