using Core.Application.Interfaces;
using Domain.Common.Events.Customers;
using Domain.Common.Events.Orders;
using Domain.Orders.Entities;
using MassTransit;
using Application.Observability;

namespace Infrastructure.Clients;

public sealed class OrderMessagingClient : IOrderMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishOrderSubmitted(Order order, Guid basketClientId, CancellationToken ct = default)
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
            ReservationId = order.Stock?.ReservationId ?? Guid.Empty,
            DestinationAddress = FormatAddress(order.Customer?.Address)
        }, context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = order.Id;
        }, ct);
    }

    private static string FormatAddress(OrderAddressInfo? address)
    {
        if (address is null) return string.Empty;
        
        var parts = new[]
        {
            address.Street,
            address.Number,
            address.AdditionalInformation,
            address.ZipCode,
            address.City
        }.Where(s => !string.IsNullOrWhiteSpace(s));
        
        return string.Join(", ", parts);
    }

    public Task PublishOrderCancelled(Guid orderId, string customerEmail, string reason, CancellationToken ct = default)
    {
        var messageId = OrderMessageIdentity.Create(orderId, Guid.Empty, "order-cancelled");
        return _publishEndpoint.Publish(new OrderCancelledEvent
        {
            EventId = messageId,
            CorrelationId = orderId,
            OrderId = orderId,
            CustomerEmail = customerEmail,
            Reason = reason
        }, context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = orderId;
        }, ct);
    }

    public Task PublishCustomerAddressUpdated(Guid customerId, OrderAddressInfo address, Guid orderId, CancellationToken ct = default)
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
        }, context =>
        {
            context.MessageId = messageId;
            context.CorrelationId = orderId;
        }, ct);
    }
}
