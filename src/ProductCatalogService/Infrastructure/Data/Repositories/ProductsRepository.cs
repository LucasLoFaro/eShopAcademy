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
        ICassandraDatabaseClient _cassandraSessionProvider;
        private readonly Table<Product> _products;
        Mapper _mapper;
        public ProductsRepository(ICassandraDatabaseClient cassandraSessionProvider)
        {
            _cassandraSessionProvider = cassandraSessionProvider;
            _cassandraSessionProvider.OpenConnection();
            _products = new Table<Product>(_cassandraSessionProvider.GetSession());
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _products.Select(p => p).ExecuteAsync();
        }
        public async Task<Product> GetByIdAsync(Guid productId)
        {
            return await _products.Where(p => p.ProductId == productId).AllowFiltering().FirstOrDefault().ExecuteAsync(); //pending replace allowquery with corresponding query
        }

        public async Task AddAsync(Product product)
        {
            await _products.Insert(product).ExecuteAsync();
        }

        public async Task UpdateAsync(string productId, Product updatedProduct)
        {
            IEnumerable<Product> products = _mapper.Fetch<Product>($"SELECT * FROM Products WHERE ProductId = ?", productId).ToList();
            Product product = products.FirstOrDefault();
            _mapper.Update(product);
        }

        public async Task DeleteAsync(Product product)
        {
            await _products.Where(p => p.ProductId == product.ProductId).AllowFiltering().Delete().ExecuteAsync();
        }

        public async Task<Product> GetMostExpensive()
        {
            return await _products.OrderByDescending(p => p.Price).AllowFiltering().FirstOrDefault().ExecuteAsync();
        }
    }

}
