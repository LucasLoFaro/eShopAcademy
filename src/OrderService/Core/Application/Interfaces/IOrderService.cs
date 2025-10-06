using Core.Domain.Contracts;
using Core.Domain.Entities;
using Domain.Contracts;


namespace Core.Application.Interfaces;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(OrderRequest request);
    Task<List<Order>> GetAllOrders();
    Task<Order?> GetOrderById(Guid id);
    Task RemoveOrder(Guid id);
}
