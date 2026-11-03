using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Domain.Entities;

public sealed class Product
{
    public Product(Guid id, string name, decimal price, int stock)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El producto es requerido.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre del producto es requerido.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "El precio del producto no puede ser negativo.");
        }

        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "El stock del producto no puede ser negativo.");
        }

        Id = id;
        Name = name.Trim();
        Price = price;
        Stock = stock;
    }

    public Guid Id { get; }

    public string Name { get; }

    public decimal Price { get; }

    public int Stock { get; private set; }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor que cero.");
        }

        if (quantity > Stock)
        {
            throw new DomainValidationException("Stock insuficiente.");
        }

        Stock -= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor que cero.");
        }

        Stock += quantity;
    }
}
