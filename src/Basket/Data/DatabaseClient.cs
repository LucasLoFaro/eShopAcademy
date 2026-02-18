using Data.Interfaces;
using StackExchange.Redis;

namespace Data
{
    public class DatabaseClient : IDatabaseClient
    {
        ConnectionMultiplexer redis;
        IDatabase db;

        public DatabaseClient(string connectionString)
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            redis = ConnectionMultiplexer.Connect(options);
            db = redis.GetDatabase();
        }

        public IDatabase GetDatabase()
            => db;
    }
}
