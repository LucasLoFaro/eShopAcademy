using Domain.Order.Contracts;
using Domain.Order.Entities;


namespace Core.Application.Interfaces;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(OrderRequest request, CancellationToken ct = default);
    Task<List<Order>> GetAllOrders(CancellationToken ct = default);
    Task<Order?> GetOrderById(Guid id, CancellationToken ct = default);
    Task RemoveOrder(Guid id, CancellationToken ct = default);
}
