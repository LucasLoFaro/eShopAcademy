using Domain.Orders.Entities;

namespace Core.Application.Interfaces;

public interface IOrderMessagingClient
{
    Task PublishOrderSubmitted(Order order, CancellationToken ct = default);
    Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken ct = default);
}