using Domain.Products.Contracts;
using Domain.Products.Entities;

namespace Core.Application.Interfaces.Services;

public interface IProductService
{
    Task AddOrUpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetMostExpensive(CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default);
}
