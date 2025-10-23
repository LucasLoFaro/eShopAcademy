using Domain.Orders.Entities;
using Domain.Orders.Contracts;

namespace Core.Application.Interfaces;

public interface IStockServiceClient
{
    Task<ReserveStockResponse> ReserveStockAsync(Guid orderId, List<Item> items, CancellationToken ct);
    Task<CommitReservationResponse> CommitReservationAsync(Guid orderId, Guid reservationId, CancellationToken ct);
    Task<CancelReservationResponse> CancelReservationAsync(Guid orderId, Guid reservationId, CancellationToken ct);
}