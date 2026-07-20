using Domain.Common.Commands.Orders;
using Domain.Common.States;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Orchestration.Data;

namespace Orders.Tests.Orchestration.Saga;

public sealed class OrderSagaPersistenceTests
{
    [Fact]
    public void Postgres_model_contains_outbox_and_database_generated_concurrency_token()
    {
        using var db = CreatePostgresModelContext();

        Assert.NotNull(db.Model.FindEntityType(typeof(InboxState)));
        Assert.NotNull(db.Model.FindEntityType(typeof(OutboxState)));
        Assert.NotNull(db.Model.FindEntityType(typeof(OutboxMessage)));

        var saga = db.Model.FindEntityType(typeof(OrderState));
        Assert.NotNull(saga);
        Assert.Null(saga.FindProperty(nameof(OrderState.RowVersion)));

        var version = saga.FindProperty("Version");
        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, version.ValueGenerated);
        Assert.Equal("xmin", version.GetColumnName());
        Assert.Equal("xid", version.GetColumnType());
    }

    [Fact]
    public async Task Stale_saga_version_raises_optimistic_concurrency_conflict()
    {
        var databasePath = TempDatabasePath();
        try
        {
            var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            await using (var setup = new TestOutboxDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.OrderStates.Add(NewState(Guid.Parse("40000000-0000-0000-0000-000000000001")));
                await setup.SaveChangesAsync();
            }

            await using (var staleContext = new TestOutboxDbContext(options))
            {
                var stale = await staleContext.OrderStates.SingleAsync();

                await using (var concurrentContext = new TestOutboxDbContext(options))
                {
                    await concurrentContext.Database.ExecuteSqlRawAsync(
                        "UPDATE order_saga_state SET Version = Version + 1");
                }

                stale.CurrentState = "Processing";
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Transaction_rollback_removes_saga_update_and_outgoing_message()
    {
        var databasePath = TempDatabasePath();
        var provider = BuildOutboxProvider(databasePath);
        try
        {
            await EnsureCreated(provider);

            await using (var scope = provider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.OrderStates.Add(NewState(Guid.Parse("40000000-0000-0000-0000-000000000002")));
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>()
                    .Publish(Outgoing(Guid.Parse("40000000-0000-0000-0000-000000000002")));
                await db.SaveChangesAsync();
                await transaction.RollbackAsync();
            }

            await using var verificationScope = provider.CreateAsyncScope();
            var verification = verificationScope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
            Assert.Equal(0, await verification.OrderStates.CountAsync());
            Assert.Equal(0, await verification.Set<OutboxMessage>().CountAsync());
        }
        finally
        {
            await provider.DisposeAsync();
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Persisted_outbox_message_is_dispatched_after_bus_restart()
    {
        var databasePath = TempDatabasePath();
        var orderId = Guid.Parse("40000000-0000-0000-0000-000000000003");
        try
        {
            await using (var crashedProcess = BuildOutboxProvider(databasePath))
            {
                await EnsureCreated(crashedProcess);
                await using var scope = crashedProcess.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.OrderStates.Add(NewState(orderId));
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(Outgoing(orderId));
                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                Assert.Equal(1, await db.OrderStates.CountAsync());
                Assert.Equal(1, await db.Set<OutboxMessage>().CountAsync());
            }

            await using var restartedProcess = BuildOutboxProvider(databasePath);
            var harness = restartedProcess.GetRequiredService<ITestHarness>();
            await harness.Start();
            try
            {
                Assert.True(await harness.Consumed.Any<UpdateOrderStatusCommand>(
                    x => x.Context.Message.OrderId == orderId));

                await using var scope = restartedProcess.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
                await WaitUntilAsync(async () => await db.Set<OutboxMessage>().CountAsync() == 0);
                Assert.Equal(1, await db.OrderStates.CountAsync());
            }
            finally
            {
                await harness.Stop();
            }
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public async Task Process_stop_after_commit_keeps_transition_and_outgoing_message()
    {
        var databasePath = TempDatabasePath();
        var orderId = Guid.Parse("40000000-0000-0000-0000-000000000004");
        try
        {
            await using (var process = BuildOutboxProvider(databasePath))
            {
                await EnsureCreated(process);
                await using var scope = process.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.OrderStates.Add(NewState(orderId));
                await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(Outgoing(orderId));
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                // The harness is deliberately never started, modelling a stop before dispatch.
            }

            var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;
            await using var verification = new TestOutboxDbContext(options);
            Assert.Equal(1, await verification.OrderStates.CountAsync(x => x.OrderId == orderId));
            Assert.Equal(1, await verification.Set<OutboxMessage>().CountAsync());
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static OrderSagaDbContext CreatePostgresModelContext()
    {
        var options = new DbContextOptionsBuilder<OrderSagaDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=postgres;Password=postgres")
            .Options;
        return new OrderSagaDbContext(options);
    }

    private static ServiceProvider BuildOutboxProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestOutboxDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath};Pooling=False"));
        services.AddMassTransitTestHarness(configurator =>
        {
            configurator.AddConsumer<RecoveredCommandConsumer>();
            configurator.AddEntityFrameworkOutbox<TestOutboxDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.QueryDelay = TimeSpan.FromMilliseconds(10);
                outbox.UseBusOutbox(busOutbox =>
                    busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(2));
            });
        });
        return services.BuildServiceProvider(true);
    }

    private static async Task EnsureCreated(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestOutboxDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    private static OrderState NewState(Guid orderId) => new()
    {
        CorrelationId = orderId,
        OrderId = orderId,
        CurrentState = "Submitted",
        CustomerId = Guid.Parse("50000000-0000-0000-0000-000000000001"),
        BasketClientId = Guid.Parse("50000000-0000-0000-0000-000000000002"),
        PaymentId = Guid.Parse("50000000-0000-0000-0000-000000000003"),
        ReservationId = Guid.Parse("50000000-0000-0000-0000-000000000004"),
        CustomerName = "Persistence Test",
        CustomerEmail = "persistence@example.invalid",
        DestinationAddress = "Persistence test address",
        ProviderTransactionId = string.Empty,
        TotalAmount = 10m
    };

    private static UpdateOrderStatusCommand Outgoing(Guid orderId) => new()
    {
        EventId = Guid.Parse("50000000-0000-0000-0000-000000000005"),
        OrderId = orderId,
        Status = "Paid"
    };

    private static string TempDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"order-saga-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        Assert.True(await condition(), "Condition was not satisfied before the deterministic timeout.");
    }

    private sealed class RecoveredCommandConsumer : IConsumer<UpdateOrderStatusCommand>
    {
        public Task Consume(ConsumeContext<UpdateOrderStatusCommand> context) => Task.CompletedTask;
    }

    private sealed class TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options)
        : SagaDbContext(options)
    {
        protected override IEnumerable<ISagaClassMap> Configurations => [new OrderStateMap()];

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<OrderState>()
                .Property<uint>("Version")
                .IsConcurrencyToken()
                .ValueGeneratedNever()
                .HasDefaultValue(0u);
            modelBuilder.AddTransactionalOutboxEntities();
        }

        public DbSet<OrderState> OrderStates => Set<OrderState>();
    }
}
