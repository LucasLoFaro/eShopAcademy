using Core.Application.Interfaces.Data;
using Core.Application.Interfaces.Services;
using Domain.Products.Contracts;
using Domain.Products.Entities;

namespace gRPC.Services;

public sealed class ProductQueryService : IProductService
{
    private readonly IProductsRepository _repository;

    public ProductQueryService(IProductsRepository repository) => _repository = repository;

    public Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.GetByIdAsync(id, cancellationToken);

    public Task<Product?> GetMostExpensive(CancellationToken cancellationToken = default) =>
        _repository.GetMostExpensive(cancellationToken);

    public Task<PagedResult<Product>> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken = default) =>
        _repository.SearchAsync(filter, cancellationToken);

    public Task AddOrUpdateAsync(Product product, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The product gRPC service is read-only.");

    public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The product gRPC service is read-only.");
}
