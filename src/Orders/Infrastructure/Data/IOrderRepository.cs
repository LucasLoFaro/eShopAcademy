using Domain.Orders.Entities;


namespace Data;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> RemoveByIdAsync(Guid id, CancellationToken ct = default);
}