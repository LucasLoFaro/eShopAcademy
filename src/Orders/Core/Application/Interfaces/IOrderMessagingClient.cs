using Domain.Orders.Entities;


namespace Core.Application.Interfaces;

public interface IOrderMessagingClient
{
    Task PublishOrderSubmitted(Order order);
}