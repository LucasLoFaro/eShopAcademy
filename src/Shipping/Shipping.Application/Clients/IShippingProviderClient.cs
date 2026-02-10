using Domain.Shipping.Contracts.Requests;
using Domain.Shipping.Contracts.Responses;

namespace Shipping.Application.Clients;

public interface IShippingProviderClient
{
    Task ScheduleShippingAsync(Domain.Shipping.Entities.Shipping shipping, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShippingStatusResponse>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task ConfirmPickupAsync(Guid shippingId, Guid orderId, DateTime readyAt, CancellationToken cancellationToken = default);
}
