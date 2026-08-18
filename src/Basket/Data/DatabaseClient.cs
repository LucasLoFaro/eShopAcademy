using Data.Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Data;

public sealed class DatabaseClient : IDatabaseClient, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _database;

    public DatabaseClient(IOptions<BasketRedisOptions> options)
    {
        var configuration = ConfigurationOptions.Parse(options.Value.ConnectionString);
        configuration.AbortOnConnectFail = false;
        configuration.ConnectTimeout = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
        configuration.SyncTimeout = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
        _redis = ConnectionMultiplexer.Connect(configuration);
        _database = _redis.GetDatabase();
    }

    public IDatabase GetDatabase() => _database;

    public Task<TimeSpan> PingAsync(CancellationToken cancellationToken = default)
        => _database.PingAsync().WaitAsync(cancellationToken);

    public void Dispose() => _redis.Dispose();
}
