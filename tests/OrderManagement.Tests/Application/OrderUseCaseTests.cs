using OrderManagement.Application.DTOs;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Services;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Tests.Application;

public sealed class OrderUseCaseTests
{
    [Fact]
    public async Task CreateOrder_UsesCatalogPriceAndConfirmsApprovedPayment()
    {
        var product = CreateProduct(price: 12.50m, stock: 5);
        var fixture = new ApplicationFixture(product, new PaymentResult(PaymentStatus.Approved));

        var result = await fixture.CreateUseCase.ExecuteAsync(CreateRequest(product.Id, 2));

        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Equal(25m, result.TotalAmount);
        Assert.Equal(12.50m, result.Items.Single().UnitPrice);
        Assert.Empty(fixture.Inventory.ReleasedReservations);
        Assert.Single(fixture.Notifications.Notifications);
    }

    [Fact]
    public async Task CreateOrder_ReleasesReservationWhenPaymentIsRejected()
    {
        var product = CreateProduct(stock: 5);
        var fixture = new ApplicationFixture(product, new PaymentResult(PaymentStatus.Rejected));

        var result = await fixture.CreateUseCase.ExecuteAsync(CreateRequest(product.Id, 1));

        Assert.Equal(OrderStatus.PaymentFailed, result.Status);
        Assert.Single(fixture.Inventory.ReleasedReservations);
    }

    [Fact]
    public async Task CreateOrder_DoesNotReleaseReservationWhenPaymentHasTechnicalError()
    {
        var product = CreateProduct(stock: 5);
        var fixture = new ApplicationFixture(product, new PaymentResult(PaymentStatus.TechnicalError));

        await Assert.ThrowsAsync<PaymentProcessingException>(
            () => fixture.CreateUseCase.ExecuteAsync(CreateRequest(product.Id, 1)));

        Assert.Empty(fixture.Inventory.ReleasedReservations);
        Assert.Equal(OrderStatus.PendingPayment, fixture.Orders.Single().Status);
    }

    [Fact]
    public async Task CreateOrder_NotificationFailureDoesNotRollbackApprovedPayment()
    {
        var product = CreateProduct(stock: 5);
        var fixture = new ApplicationFixture(product, new PaymentResult(PaymentStatus.Approved))
        {
            NotificationException = new InvalidOperationException("Notificación no disponible.")
        };

        var result = await fixture.CreateUseCase.ExecuteAsync(CreateRequest(product.Id, 1));

        Assert.Equal(OrderStatus.Confirmed, result.Status);
        Assert.Empty(fixture.Inventory.ReleasedReservations);
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFoundForUnknownOrder()
    {
        var fixture = new ApplicationFixture(CreateProduct(), new PaymentResult(PaymentStatus.Approved));

        await Assert.ThrowsAsync<OrderNotFoundException>(
            () => fixture.GetUseCase.ExecuteAsync(Guid.NewGuid()));
    }

    private static Product CreateProduct(decimal price = 10m, int stock = 1)
    {
        return new Product(Guid.NewGuid(), "Teclado", price, stock);
    }

    private static CreateOrderRequest CreateRequest(Guid productId, int quantity)
    {
        return new CreateOrderRequest
        {
            CustomerEmail = "cliente@ejemplo.com",
            Items = [new CreateOrderItemRequest { ProductId = productId, Quantity = quantity }]
        };
    }

    private sealed class ApplicationFixture
    {
        public ApplicationFixture(Product product, PaymentResult paymentResult)
        {
            Products = new FakeProductRepository(product);
            Orders = new FakeOrderRepository();
            Inventory = new FakeInventoryRepository();
            Payment = new FakePaymentGateway(paymentResult);
            Notifications = new FakeNotificationService(this);
            CreateUseCase = new CreateOrderUseCase(Products, Orders, Inventory, Payment, Notifications);
            GetUseCase = new GetOrderUseCase(Orders);
        }

        public FakeProductRepository Products { get; }
        public FakeOrderRepository Orders { get; }
        public FakeInventoryRepository Inventory { get; }
        public FakePaymentGateway Payment { get; }
        public FakeNotificationService Notifications { get; }
        public CreateOrderUseCase CreateUseCase { get; }
        public GetOrderUseCase GetUseCase { get; }
        public Exception? NotificationException { get; set; }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly IReadOnlyCollection<Product> products;

        public FakeProductRepository(params Product[] products)
        {
            this.products = products;
        }

        public Task<IReadOnlyCollection<Product>> GetByIdsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken = default)
        {
            var result = products.Where(product => productIds.Contains(product.Id)).ToArray();
            return Task.FromResult<IReadOnlyCollection<Product>>(result);
        }

        public Task<IReadOnlyCollection<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(products);
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public List<Order> Orders { get; } = [];

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            Orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Orders.SingleOrDefault(order => order.Id == orderId));
        }

        public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Order Single() => Orders.Single();
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        public List<Guid> ReleasedReservations { get; } = [];

        public Task<InventoryReservation> ReserveAsync(
            IReadOnlyCollection<InventoryReservationItem> items,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new InventoryReservation(Guid.NewGuid()));
        }

        public Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            ReleasedReservations.Add(reservationId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentGateway : IPaymentGateway
    {
        private readonly PaymentResult result;

        public FakePaymentGateway(PaymentResult result)
        {
            this.result = result;
        }

        public Task<PaymentResult> ProcessPaymentAsync(Order order, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        private readonly ApplicationFixture fixture;

        public FakeNotificationService(ApplicationFixture fixture)
        {
            this.fixture = fixture;
        }

        public List<Guid> Notifications { get; } = [];

        public Task NotifyOrderResultAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (fixture.NotificationException is not null)
            {
                throw fixture.NotificationException;
            }

            Notifications.Add(order.Id);
            return Task.CompletedTask;
        }
    }
}
