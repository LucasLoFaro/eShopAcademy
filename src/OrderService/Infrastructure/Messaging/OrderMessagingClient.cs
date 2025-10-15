using Core.Application.Interfaces;
using Domain.Order.Entities;
using Domain.Common.Events;
using MassTransit;


namespace Infrastructure.Services;

public class OrderMessagingClient : IOrderMessagingClient
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderMessagingClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }


    public async Task PublishOrderSubmitted(Order order)
        => await _publishEndpoint.Publish(new OrderSubmittedEvent() 
        {
            OrderId = order.Id,
            PaymentId = order.PaymentId,
            CustomerEmail = order.Customer.Mail,
            ReservationId = order.ReservationId
        });
}