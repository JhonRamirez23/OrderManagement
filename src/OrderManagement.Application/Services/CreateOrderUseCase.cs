using OrderManagement.Application.DTOs;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services;

public sealed class CreateOrderUseCase
{
    private readonly IProductRepository productRepository;
    private readonly IOrderRepository orderRepository;
    private readonly IInventoryRepository inventoryRepository;
    private readonly IPaymentGateway paymentGateway;
    private readonly INotificationService notificationService;

    public CreateOrderUseCase(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IPaymentGateway paymentGateway,
        INotificationService notificationService)
    {
        this.productRepository = productRepository;
        this.orderRepository = orderRepository;
        this.inventoryRepository = inventoryRepository;
        this.paymentGateway = paymentGateway;
        this.notificationService = notificationService;
    }

    public async Task<OrderResponse> ExecuteAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var requestedItems = request.Items.ToArray();
        var productIds = requestedItems.Select(item => item.ProductId).ToArray();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        foreach (var productId in productIds)
        {
            if (!productsById.ContainsKey(productId))
            {
                throw new ProductNotFoundException(productId);
            }
        }

        var orderItems = requestedItems
            .Select(item =>
            {
                var product = productsById[item.ProductId];
                return new OrderItem(product.Id, product.Name, product.Price, item.Quantity);
            })
            .ToArray();

        var reservationItems = requestedItems
            .Select(item => new InventoryReservationItem(item.ProductId, item.Quantity))
            .ToArray();
        var reservation = await inventoryRepository.ReserveAsync(reservationItems, cancellationToken);
        var order = new Order(Guid.NewGuid(), request.CustomerEmail, orderItems);

        try
        {
            await orderRepository.AddAsync(order, cancellationToken);
        }
        catch
        {
            await inventoryRepository.ReleaseAsync(reservation.ReservationId, cancellationToken);
            throw;
        }

        PaymentResult paymentResult;
        try
        {
            paymentResult = await paymentGateway.ProcessPaymentAsync(order, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new PaymentProcessingException(order.Id, exception.Message);
        }

        if (paymentResult.Status == PaymentStatus.TechnicalError)
        {
            throw new PaymentProcessingException(order.Id, paymentResult.Message);
        }

        if (paymentResult.Status == PaymentStatus.Rejected)
        {
            await inventoryRepository.ReleaseAsync(reservation.ReservationId, cancellationToken);
            order.TransitionTo(OrderStatus.PaymentFailed);
            await orderRepository.UpdateAsync(order, cancellationToken);
            await TryNotifyAsync(order, cancellationToken);
            return Map(order);
        }

        order.TransitionTo(OrderStatus.Confirmed);
        await orderRepository.UpdateAsync(order, cancellationToken);
        await TryNotifyAsync(order, cancellationToken);
        return Map(order);
    }

    private static void ValidateRequest(CreateOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            throw new ApplicationValidationException("Customer email is required.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApplicationValidationException("At least one order item is required.");
        }

        if (request.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0))
        {
            throw new ApplicationValidationException("Each item must have a valid product and a quantity greater than zero.");
        }

        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
        {
            throw new ApplicationValidationException("A product cannot appear more than once in an order.");
        }
    }

    private async Task TryNotifyAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.NotifyOrderResultAsync(order, cancellationToken);
        }
        catch (Exception)
        {
        }
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
