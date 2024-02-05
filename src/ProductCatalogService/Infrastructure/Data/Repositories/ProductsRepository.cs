using Cassandra;
using Domain.Entities;
using Application.Interfaces.Data;
using Data.Interfaces;
using Cassandra.Mapping;

namespace Data.Repositories
{

    public class ProductsRepository : IProductsRepository
    {
        ICassandraDatabaseClient _cassandraSessionProvider;
        private readonly ISession _session;
        Mapper _mapper;
        public ProductsRepository(ICassandraDatabaseClient cassandraSessionProvider)
        {
            _cassandraSessionProvider = cassandraSessionProvider;
            _cassandraSessionProvider.OpenConnection();
            _session = _cassandraSessionProvider.GetSession();
            _mapper = new Mapper(_session);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            List<Product> products = _mapper.Fetch<Product>("SELECT * FROM ProductCatalog").ToList();

            return products;
        }
        public async Task<Product> GetByIdAsync(string productId)
        {
            IEnumerable<Product> products = _mapper.Fetch<Product>($"SELECT * FROM ProductCatalog WHERE ProductId = ?", productId).ToList();

            return products?.FirstOrDefault();
        }

        public async Task AddAsync(Product product)
        {
            await _mapper.InsertAsync(product);
        }

        public async Task UpdateAsync(string productId, Product updatedProduct)
        {
            IEnumerable<Product> products = _mapper.Fetch<Product>($"SELECT * FROM ProductCatalog WHERE ProductId = ?", productId).ToList();
            Product product = products.FirstOrDefault();
            _mapper.Update(product);
        }

        public async Task DeleteAsync(Guid productId)
        {
            IEnumerable<Product> products = _mapper.Fetch<Product>($"SELECT * FROM ProductCatalog WHERE ProductId = ?", productId).ToList();
            Product product = products.FirstOrDefault();
            await _mapper.DeleteAsync<Product>(product);
        }

        public async Task<Product> GetMostExpensive()
        {
           Product product = _mapper.Fetch<Product>("SELECT * FROM ProductCatalog ORDER BY Price DESC LIMIT 1;").ToList().FirstOrDefault();
            return product;
        }
    }

}
