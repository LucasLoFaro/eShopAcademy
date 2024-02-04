using Domain;

namespace Data.Interfaces
{
    public interface IProductRepository
    {
        public Task<bool> AddOrUpdateProduct(Product product);
    }
}