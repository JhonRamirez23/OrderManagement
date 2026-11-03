namespace OrderManagement.Application.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryReservation> ReserveAsync(
        IReadOnlyCollection<InventoryReservationItem> items,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);
}

public sealed record InventoryReservation(Guid ReservationId);

public sealed record InventoryReservationItem(Guid ProductId, int Quantity);
