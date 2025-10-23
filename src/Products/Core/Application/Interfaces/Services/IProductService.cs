using Domain.Products.Entities;

namespace Core.Application.Interfaces.Services;

public interface IProductService
{
    Task AddOrUpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetMostExpensive();
}