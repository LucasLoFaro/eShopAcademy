using Cassandra;
using Cassandra.Mapping;
using Data.Interfaces;
using Data.Settings;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Data
{
    public class DataStaxDatabaseClient : ICassandraDatabaseClient
    {
        private Cluster _cluster;
        private ISession _session;
        IOptionsMonitor<DatabaseSettings> _settings;

        public DataStaxDatabaseClient(IOptionsMonitor<DatabaseSettings> databaseSettings)
        {
            _settings = databaseSettings;
        }
        public void OpenConnection()
        {
            _cluster = Cluster.Builder()
                     .WithConnectionString("Contact Points=cassandra;Port=9042;Username=Cassandra;Password=Cassandra;Keyspace=products_keyspace;")
                     .Build();
            _session = _cluster.Connect(_settings.CurrentValue.KeySpace);

            MappingConfiguration.Global.Define(
            new Map<Product>()
            .TableName("products_by_category")
            .PartitionKey(p => p.ProductId)
            .Column(p => p.ProductId, c => c.WithName("productID")));
        }

        public void Dispose()
        {
            _session?.Dispose();
            _cluster?.Dispose();
        }

        public ISession GetSession()
        {
            return _session;
        }

    }
}
