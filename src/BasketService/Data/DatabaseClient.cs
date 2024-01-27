using Data.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace Data
{
    public class DatabaseClient : IDatabaseClient
    {
        ConnectionMultiplexer redis;
        IDatabase db;

        public DatabaseClient(IOptionsMonitor<DatabaseSettings> settings)
        {
            redis = ConnectionMultiplexer.Connect(settings.CurrentValue.URL);
            db = redis.GetDatabase();
        }

        public IDatabase GetDatabase()
        {
            return db;
        }
    }
}
