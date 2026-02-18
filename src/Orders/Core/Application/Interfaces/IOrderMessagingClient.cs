using Domain.Orders.Entities;

namespace Core.Application.Interfaces;

public interface IOrderMessagingClient
{
    Task PublishOrderSubmitted(Order order, Guid basketClientId, CancellationToken ct = default);
    Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken ct = default);
    Task PublishCustomerAddressUpdated(Guid customerId, OrderAddressInfo address, Guid orderId, CancellationToken ct = default);
}