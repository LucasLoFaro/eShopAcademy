using StackExchange.Redis;

namespace Data.Interfaces
{
    public interface IDatabaseClient
    {
        public IDatabase GetDatabase();
    }
}