using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(
        Order order,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentResult(PaymentStatus Status, string? Message = null);

public enum PaymentStatus
{
    Approved,
    Rejected,
    TechnicalError
}
