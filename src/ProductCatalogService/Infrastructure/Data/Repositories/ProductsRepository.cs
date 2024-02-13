using Cassandra;
using Domain.Entities;
using Application.Interfaces.Data;
using Data.Interfaces;
using Cassandra.Mapping;
using Cassandra.Data.Linq;

namespace Data.Repositories
{

    public class ProductsRepository : IProductsRepository
    {
        private readonly Table<Product> _products;
        
        public ProductsRepository(ICassandraDatabaseClient cassandraSessionProvider)
        {
            _products = new Table<Product>(cassandraSessionProvider.Session);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _products.Select(p => p).ExecuteAsync();
        }
        public async Task<Product> GetByIdAsync(Guid id)
        {
            return await _products.Where(p => p.ID == id).FirstOrDefault().ExecuteAsync();
        }

        public async Task<Product> GetMostExpensive()
        {
            return await _products.OrderByDescending(p => p.Price).FirstOrDefault().ExecuteAsync();
        }
        
        public async Task AddAsync(Product product)
        {
            await _products.Insert(product).ExecuteAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            await _products.Where(p => p.ID == product.ID).Delete().ExecuteAsync();
        }
    }

}
