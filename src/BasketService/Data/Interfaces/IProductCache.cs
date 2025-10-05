using Core.Domain.Contracts;
using Core.Domain.Entities;
using Domain.DTOs;

namespace Data.Interfaces
{
    public interface IProductCache
    {
        public Task<bool> AddOrUpdateProduct(ProductDTO product);
        public Task<bool> UpdateProductStock(AlterStockDTO stock);
    }
}