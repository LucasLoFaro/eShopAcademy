using Domain.DTOs;
using Domain.Entities;

namespace Data.Interfaces
{
    public interface IProductCache
    {
        public Task<bool> AddOrUpdateProduct(ProductDTO product);
        public Task<bool> UpdateProductStock(ProductStockDTO stock);
    }
}