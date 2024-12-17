using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NRedisStack;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Data
{
    public class RedisDatabaseClient : IRedisDatabaseClient
    {
        private ConnectionMultiplexer _cluster;
        private ConfigurationOptions _options;
        public IDatabase _db;
        public RedisDatabaseClient(IConfiguration configuration)
        {
            string connectionString = configuration.GetSection("DatabaseSettings:ConnectionString").Value ?? throw new Exception("null connectionString");
            _options = ConfigurationOptions.Parse(connectionString);
            OpenConnection();
        }
        public void Dispose()
        {
            _cluster.Dispose();
        }

        public IDatabase GetSession()
        {
            return _cluster.GetDatabase();
        }

        public void OpenConnection()
        {
            _cluster = ConnectionMultiplexer.Connect(_options);
        }
    }
}
