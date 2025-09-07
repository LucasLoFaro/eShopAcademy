using Cassandra;
using Cassandra.Mapping;
using Data.Interfaces;
using Core.Domain.Entities;

namespace Data
{
    public class DataStaxDatabaseClient : ICassandraDatabaseClient
    {
        private Cluster Cluster;
        public ISession Session {  get; private set; }

        public DataStaxDatabaseClient(string connectionString, string keySpace)
        {
            Cluster = Cluster.Builder()
                     .AddContactPoints(connectionString)
                     .Build();
            
            SetupDBStructure();
            Session = Cluster.Connect(keySpace);

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
                " WITH replication = { 'class': 'SimpleStrategy', 'replication_factor': '1' };"));
            Session.Execute(new SimpleStatement("USE eshopacademy;"));
            Session.Execute(new SimpleStatement("CREATE TABLE IF NOT EXISTS eshopacademy.Products(" +
                "ID UUID PRIMARY KEY," +
                "Name text," +
                "Price float," +
                "Stock int," +
                "Description text," +
                "Image text," +
                "CategoryName text);"));
            Session.Execute(new SimpleStatement("CREATE TABLE IF NOT EXISTS eshopacademy.Categories(" +
                "ID UUID PRIMARY KEY," +
                "Name text);"));
        }

        public void Dispose()
        {
            Session?.Dispose();
            Cluster?.Dispose();
        }
    }
}
