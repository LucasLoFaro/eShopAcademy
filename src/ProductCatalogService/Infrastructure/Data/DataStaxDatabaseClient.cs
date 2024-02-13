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
        private Cluster Cluster;
        public ISession Session {  get; private set; }

        public DataStaxDatabaseClient(IOptionsMonitor<DatabaseSettings> dbSettings)
        {
            Cluster = Cluster.Builder()
                     .AddContactPoints(dbSettings.CurrentValue.Host)
                     .Build();
            
            SetupDBStructure();
            Session = Cluster.Connect(dbSettings.CurrentValue.KeySpace);

            //This is like a db context / object relational mapping
            MappingConfiguration.Global.Define(
                new Map<Product>()
                .TableName("Products")
                .PartitionKey(p => p.ID)
                .Column(p => p.ID, c => c.WithName("ID")));
        }

        private void SetupDBStructure()
        {
            Session = Cluster.Connect();
            Session.Execute(new SimpleStatement("CREATE KEYSPACE IF NOT EXISTS eshopacademy" +
                " WITH replication = { 'class': 'SimpleStrategy', 'replication_factor': '1' }"));
            Session.Execute(new SimpleStatement("USE eshopacademy"));
            Session.Execute(new SimpleStatement("CREATE TABLE IF NOT EXISTS eshopacademy.Products(" +
                "ID UUID PRIMARY KEY," +
                "Name text," +
                "Price float," +
                "Description text," +
                "Image text," +
                "CategoryDescription text)"));
        }

        public void Dispose()
        {
            Session?.Dispose();
            Cluster?.Dispose();
        }
    }
}
