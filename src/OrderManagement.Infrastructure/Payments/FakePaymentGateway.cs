using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Infrastructure.Payments;

public sealed class FakePaymentGateway : IPaymentGateway
{
    private readonly PaymentResult result;

    public FakePaymentGateway(
        PaymentStatus status = PaymentStatus.Approved,
        string? message = null)
    {
        result = new PaymentResult(status, message);
    }

    public Task<PaymentResult> ProcessPaymentAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(result);
    }
}
