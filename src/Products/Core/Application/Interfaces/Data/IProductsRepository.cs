using Domain.Products.Contracts;
using Domain.Products.Entities;

namespace Core.Application.Interfaces.Data;

public interface IProductsRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetMostExpensive(CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default);
}
