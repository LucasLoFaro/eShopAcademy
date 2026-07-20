using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orchestration.Data;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Orders.Tests.Orchestration.SystemTests;

public sealed class OrderSagaSystemFactAttribute : FactAttribute
{
    public const string EnabledVariable = "RUN_ORDER_SAGA_SYSTEM_TESTS";

    public OrderSagaSystemFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(EnabledVariable),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Set {EnabledVariable}=true to run containerized order-saga system tests.";
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OrderSagaSystemCollection : ICollectionFixture<OrderSagaSystemFixture>
{
    public const string Name = "Order saga system tests";
}

public sealed class OrderSagaSystemFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17.6")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:4.3-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async Task InitializeAsync()
    {
        if (!SystemTestsEnabled())
        {
            return;
        }

        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());
    }

    public async Task DisposeAsync()
    {
        if (!SystemTestsEnabled())
        {
            return;
        }

        await _rabbit.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<OrderSagaSystemScope> CreateScopeAsync(CancellationToken cancellationToken = default)
    {
        Assert.True(SystemTestsEnabled(),
            $"Set {OrderSagaSystemFactAttribute.EnabledVariable}=true before creating a system-test scope.");

        var suffix = Guid.NewGuid().ToString("N");
        var databaseName = $"order_saga_{suffix}";
        var virtualHost = $"order_saga_{suffix}";

        await using (var connection = new NpgsqlConnection(AdminConnectionString()))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var addVirtualHost = await _rabbit.ExecAsync(
            ["rabbitmqctl", "add_vhost", virtualHost],
            cancellationToken);
        Assert.Equal(0, addVirtualHost.ExitCode);

        var setPermissions = await _rabbit.ExecAsync(
            ["rabbitmqctl", "set_permissions", "-p", virtualHost, "guest", ".*", ".*", ".*"],
            cancellationToken);
        Assert.Equal(0, setPermissions.ExitCode);

        var databaseConnectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = databaseName
        }.ConnectionString;

        var rabbitUri = new UriBuilder(_rabbit.GetConnectionString())
        {
            Path = virtualHost
        }.Uri;

        var scope = new OrderSagaSystemScope(
            this,
            databaseName,
            virtualHost,
            databaseConnectionString,
            rabbitUri,
            $"order-saga-{suffix}");

        await using var db = scope.CreateDbContext();
        await db.Database.MigrateAsync(cancellationToken);

        return scope;
    }

    internal async Task DropScopeAsync(
        string databaseName,
        string virtualHost,
        CancellationToken cancellationToken = default)
    {
        await _rabbit.ExecAsync(["rabbitmqctl", "delete_vhost", virtualHost], cancellationToken);

        await using var connection = new NpgsqlConnection(AdminConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<string> QueueSummaryAsync(
        string virtualHost,
        CancellationToken cancellationToken = default)
    {
        var result = await _rabbit.ExecAsync(
            ["rabbitmqctl", "list_queues", "-p", virtualHost, "name", "messages_ready", "messages_unacknowledged"],
            cancellationToken);
        var bindings = await _rabbit.ExecAsync(
            ["rabbitmqctl", "list_bindings", "-p", virtualHost, "source_name", "destination_name", "routing_key"],
            cancellationToken);
        return $"Queues (exit {result.ExitCode}):{Environment.NewLine}{result.Stdout}{Environment.NewLine}{result.Stderr}"
            + $"{Environment.NewLine}Bindings (exit {bindings.ExitCode}):{Environment.NewLine}{bindings.Stdout}{Environment.NewLine}{bindings.Stderr}";
    }

    private string AdminConnectionString() => new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
    {
        Database = "postgres",
        Pooling = false
    }.ConnectionString;

    private static bool SystemTestsEnabled() => string.Equals(
        Environment.GetEnvironmentVariable(OrderSagaSystemFactAttribute.EnabledVariable),
        "true",
        StringComparison.OrdinalIgnoreCase);
}

public sealed class OrderSagaSystemScope(
    OrderSagaSystemFixture fixture,
    string databaseName,
    string virtualHost,
    string databaseConnectionString,
    Uri rabbitUri,
    string schedulerName) : IAsyncDisposable
{
    public string DatabaseConnectionString { get; } = databaseConnectionString;

    public Uri RabbitUri { get; } = rabbitUri;

    public string SchedulerName { get; } = schedulerName;

    public OrderSagaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderSagaDbContext>()
            .UseNpgsql(DatabaseConnectionString)
            .Options;
        return new OrderSagaDbContext(options);
    }

    public Task<string> QueueSummaryAsync(CancellationToken cancellationToken = default) =>
        fixture.QueueSummaryAsync(virtualHost, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await fixture.DropScopeAsync(databaseName, virtualHost);
    }
}
