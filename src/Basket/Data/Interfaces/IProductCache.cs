using Domain.Basket.Contracts;

namespace Data.Interfaces;

public interface IProductCache
{
    public Task<bool> AddOrUpdateProduct(ProductDTO product);
    public Task<bool> UpdateProductStock(AlterStockDTO stock);
}