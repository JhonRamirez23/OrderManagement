using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces;

public interface INotificationService
{
    Task NotifyOrderResultAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
