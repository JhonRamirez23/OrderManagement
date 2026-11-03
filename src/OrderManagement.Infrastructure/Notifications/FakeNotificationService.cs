using System.Collections.Concurrent;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Infrastructure.Notifications;

public sealed class FakeNotificationService : INotificationService
{
    private readonly ConcurrentQueue<NotificationMessage> messages = new();

    public bool ShouldFail { get; set; }

    public IReadOnlyCollection<NotificationMessage> Messages => messages.ToArray();

    public Task NotifyOrderResultAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFail)
        {
            throw new InvalidOperationException("Notification service is unavailable.");
        }

        messages.Enqueue(new NotificationMessage(order.Id, order.CustomerEmail, order.Status));
        return Task.CompletedTask;
    }
}

public sealed record NotificationMessage(
    Guid OrderId,
    string CustomerEmail,
    OrderStatus Status);
