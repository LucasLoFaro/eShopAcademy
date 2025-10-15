using Domain.Order.Entities;


namespace Core.Application.Interfaces;

public interface IOrderMessagingClient
{
    Task PublishOrderSubmitted(Order order);
}