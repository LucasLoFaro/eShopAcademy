using Core.Domain.Entities;


namespace Data;

public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(Guid id);
    Task<bool> RemoveByIdAsync(Guid id);
}