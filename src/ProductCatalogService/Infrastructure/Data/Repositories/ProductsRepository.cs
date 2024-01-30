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

        public ProductsRepository(ICassandraDatabaseClient cassandraSessionProvider)
        {
            _cassandraSessionProvider = cassandraSessionProvider;
            _cassandraSessionProvider.OpenConnection();
            _session = _cassandraSessionProvider.GetSession();
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            var cql = "USE \"ProductCatalogMicroservice\"";
            var statement = new SimpleStatement(cql);
            await _session.ExecuteAsync(statement);

            MappingConfiguration mappingConfiguration = new MappingConfiguration();
            Mapper mapper = new Mapper(_session, mappingConfiguration);
            IEnumerable<Product> products = mapper.Fetch<Product>("SELECT * FROM ProductCatalog");
            if (products.Count() < 1)
                throw new Exception("No Products");
            List<Product> prodList = products.ToList();

            return products;
        }

        /*private Product MapToProduct(Row row)
        {
            return new Product
            {
                Id = row.GetValue<Guid>("productid"),
                Name = row.GetValue<string>("name"),
                Price = row.GetValue<decimal>("price"),
                Description = row.GetValue<string>("description"),
                ImageUrl = row.GetValue<string>("image"),
                CategoryDescription = row.GetValue<string>("categorydescription")
            };
        }*/
        
        public async Task<RowSet> ExecuteQuery(IStatement statement)
        {
            return await _session.ExecuteAsync(statement);
        }
    }

}
