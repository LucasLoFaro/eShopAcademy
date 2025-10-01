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
            redis = ConnectionMultiplexer.Connect(connectionString);
            db = redis.GetDatabase();
        }

        public IDatabase GetDatabase()
            => db;
    }
}
