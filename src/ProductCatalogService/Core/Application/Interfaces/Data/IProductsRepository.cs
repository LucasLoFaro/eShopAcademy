using Core.Domain.Entities;

namespace Core.Application.Interfaces.Data;

public interface IProductsRepository
{
    public Task<IEnumerable<Product>> GetAllAsync();
    public Task<Product?> GetByIdAsync(Guid id);
    public Task<Product?> GetMostExpensive();
    public Task AddOrUpdateAsync(Product product);
    public Task DeleteAsync(Product product);
}
