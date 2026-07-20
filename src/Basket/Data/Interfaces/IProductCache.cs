using Domain.Basket.Contracts;

namespace Data.Interfaces;

public interface IProductCache
{
    Task<bool> AddOrUpdateProduct(ProductDTO product, CancellationToken cancellationToken = default);
    Task<bool> UpdateProductStock(AlterStockDTO stock, CancellationToken cancellationToken = default);
}
