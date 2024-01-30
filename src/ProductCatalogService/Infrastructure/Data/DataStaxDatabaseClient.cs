using Cassandra;
using Application.Interfaces.Data;
using Data.Interfaces;

namespace Data
{
    public class DataStaxDatabaseClient : ICassandraDatabaseClient
    {
        private readonly IDatabaseSettingsProvider _databaseSettings;
        private Cluster _cluster;
        private ISession _session;

        public DataStaxDatabaseClient(IDatabaseSettingsProvider databaseSettings)
        {
            _databaseSettings = databaseSettings;
        }
        public void OpenConnection()
        {
            _cluster = Cluster.Builder()
           .WithCloudSecureConnectionBundle(_databaseSettings.GetConnectionBundlePath())
           .WithCredentials(_databaseSettings.GetClient(), _databaseSettings.GetSecret())
                       .Build();
            _session = _cluster.Connect();
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
