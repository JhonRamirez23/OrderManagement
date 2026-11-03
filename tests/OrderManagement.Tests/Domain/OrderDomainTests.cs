using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Tests.Domain;

public sealed class OrderDomainTests
{
    [Fact]
    public void Order_CalculatesTotalFromItsItems()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var items = new[]
        {
            new OrderItem(firstProductId, "Teclado", 25.50m, 2),
            new OrderItem(secondProductId, "Ratón", 10m, 1)
        };

        var order = new Order(Guid.NewGuid(), "cliente@ejemplo.com", items);

        Assert.Equal(61m, order.TotalAmount);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
    }

    [Fact]
    public void Product_CannotDecreaseMoreStockThanAvailable()
    {
        var product = new Product(Guid.NewGuid(), "Teclado", 25.50m, 1);

        var exception = Assert.Throws<DomainValidationException>(() => product.DecreaseStock(2));

        Assert.Equal("Stock insuficiente.", exception.Message);
        Assert.Equal(1, product.Stock);
    }

    [Fact]
    public void Order_AllowsPendingPaymentToConfirmed()
    {
        var order = CreateOrder();

        order.TransitionTo(OrderStatus.Confirmed);

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Order_RejectsTransitionFromConfirmedToPaymentFailed()
    {
        var order = CreateOrder();
        order.TransitionTo(OrderStatus.Confirmed);

        var exception = Assert.Throws<InvalidOrderTransitionException>(
            () => order.TransitionTo(OrderStatus.PaymentFailed));

        Assert.Equal(OrderStatus.Confirmed, exception.CurrentStatus);
        Assert.Equal(OrderStatus.PaymentFailed, exception.RequestedStatus);
    }

    [Fact]
    public void Order_RequiresAtLeastOneItem()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Order(Guid.NewGuid(), "cliente@ejemplo.com", []));

        Assert.Contains("al menos un artículo", exception.Message);
    }

    private static Order CreateOrder()
    {
        var item = new OrderItem(Guid.NewGuid(), "Teclado", 25.50m, 1);
        return new Order(Guid.NewGuid(), "cliente@ejemplo.com", [item]);
    }
}
