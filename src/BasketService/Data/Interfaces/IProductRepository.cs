using Domain;

namespace Data.Interfaces
{
    public interface IProductRepository
    {
        public Task<bool> AddOrUpdateProduct(ProductDTO product);
        public Task<bool> UpdateProductStock(ProductStockDTO stock);
    }
}