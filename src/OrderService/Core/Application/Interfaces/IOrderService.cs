using Core.Domain.Contracts;
using Core.Domain.Entities;
using Domain.Contracts;


namespace Core.Application.Interfaces;

public interface IOrderService
{
    Task<PlaceOrderResponse> PlaceOrderAsync(OrderRequest request, CancellationToken ct = default);
    Task<List<Order>> GetAllOrders(CancellationToken ct = default);
    Task<Order?> GetOrderById(Guid id, CancellationToken ct = default);
    Task RemoveOrder(Guid id, CancellationToken ct = default);
}
