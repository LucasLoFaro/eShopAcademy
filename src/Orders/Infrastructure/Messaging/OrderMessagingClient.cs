using Application.Observability;
using Core.Application.Interfaces;
using Domain.Common.Events.Customers;
using Domain.Common.Events.Orders;
using Domain.Orders.Entities;
using MassTransit;

namespace Infrastructure.Services;

public sealed class OrderMessagingClient : IOrderMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishOrderSubmitted(Order order, Guid basketClientId, CancellationToken cancellationToken = default)
    {
        var messageId = OrderMessageIdentity.Create(order.Id, Guid.Empty, "order-submitted");
        return _publishEndpoint.Publish(new OrderSubmittedEvent
        {
            EventId = messageId,
            CorrelationId = order.Id,
            OrderId = order.Id,
            CustomerName = order.Customer?.Name ?? string.Empty,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            CustomerId = order.CustomerId,
            BasketClientId = basketClientId,
            TotalAmount = Convert.ToDecimal(order.TotalPrice),
            PaymentId = order.Payment?.Id ?? Guid.Empty,
            ReservationId = order.Stock?.ReservationId ?? Guid.Empty
        }, context => SetHeaders(context, messageId, order.Id), cancellationToken);
    }

    public Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken cancellationToken = default)
    {
        var messageId = OrderMessageIdentity.Create(orderId, Guid.Empty, "order-cancelled");
        return _publishEndpoint.Publish(new OrderCancelledEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            OrderId = orderId,
            CustomerEmail = customerEmail,
            Reason = reason
        }, context => SetHeaders(context, messageId, orderId), cancellationToken);
    }

    public Task PublishCustomerAddressUpdated(
        Guid customerId,
        OrderAddressInfo address,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var messageId = OrderMessageIdentity.Create(orderId, Guid.Empty, "customer-address-updated");
        return _publishEndpoint.Publish(new CustomerAddressUpdatedEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            CustomerId = customerId,
            OrderId = orderId,
            Street = address.Street,
            Number = address.Number,
            AdditionalInformation = address.AdditionalInformation,
            ZipCode = address.ZipCode,
            City = address.City
        }, context => SetHeaders(context, messageId, orderId), cancellationToken);
    }

    private static void SetHeaders<T>(PublishContext<T> context, Guid messageId, Guid orderId)
        where T : class
    {
        context.MessageId = messageId;
        context.CorrelationId = orderId;
    }
}
