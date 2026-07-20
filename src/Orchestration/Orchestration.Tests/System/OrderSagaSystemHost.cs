using System.Collections.Concurrent;
using Application.Saga;
using Domain.Common.Commands;
using Domain.Common.Commands.Basket;
using Domain.Common.Commands.Operations;
using Domain.Common.Commands.Orders;
using Domain.Common.Commands.Payments;
using Domain.Common.Commands.Shipping;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Orders;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orchestration.Data;
using Quartz;

namespace Orders.Tests.Orchestration.SystemTests;

public static class OrderSagaSystemHost
{
    private static readonly Uri SchedulerEndpoint = new("queue:quartz");

    public static IHost Create(
        OrderSagaSystemScope scope,
        EffectProbe effects,
        TimeSpan paymentTimeout)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IOptions<OrderSagaOptions>>(
            Options.Create(new OrderSagaOptions { PaymentTimeout = paymentTimeout }));
        builder.Services.AddSingleton(effects);
        builder.Services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(30);
            options.StopTimeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddDbContext<OrderSagaDbContext>(options =>
            options.UseNpgsql(scope.DatabaseConnectionString));

        builder.Services.AddQuartz(quartz =>
        {
            quartz.SchedulerName = scope.SchedulerName;
            quartz.SchedulerId = "AUTO";
            quartz.UsePersistentStore(store =>
            {
                store.PerformSchemaValidation = true;
                store.UseProperties = true;
                store.UsePostgres(scope.DatabaseConnectionString);
                store.UseSystemTextJsonSerializer();
                store.UseClustering(cluster =>
                {
                    cluster.CheckinInterval = TimeSpan.FromSeconds(1);
                    cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(2);
                });
            });
        });
        builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        builder.Services.AddMassTransit(registration =>
        {
            registration.SetKebabCaseEndpointNameFormatter();
            registration.AddMessageScheduler(SchedulerEndpoint);
            registration.AddQuartzConsumers();

            registration.AddEntityFrameworkOutbox<OrderSagaDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.QueryDelay = TimeSpan.FromMilliseconds(25);
                outbox.UseBusOutbox(busOutbox =>
                    busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(5));
            });

            registration.AddSagaStateMachine<OrderStateMachine, OrderState, OrderStateMachineDefinition>()
                .EntityFrameworkRepository(repository =>
                {
                    repository.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    repository.AddDbContext<DbContext, OrderSagaDbContext>((_, options) =>
                        options.UseNpgsql(scope.DatabaseConnectionString));
                });

            registration.UsingRabbitMq((context, bus) =>
            {
                bus.Host(scope.RabbitUri);
                bus.UseMessageScheduler(SchedulerEndpoint);

                Capture<CommitStockReservationCommand>(bus, effects, "commit-stock-reservation");
                Capture<ReleaseStockReservationCommand>(bus, effects, "release-stock-reservation");
                Capture<EmptyBasketCommand>(bus, effects, "empty-basket");
                Capture<ScheduleShippingCommand>(bus, effects, "schedule-shipping");
                Capture<UpdateOrderStatusCommand>(bus, effects, "update-order-status-command");
                Capture<CancelOrderCommand>(bus, effects, "cancel-order-command");
                Capture<RefundPaymentCommand>(bus, effects, "refund-payment");
                Capture<PreparePackageCommand>(bus, effects, "prepare-package");
                Capture<ConfirmPickupCommand>(bus, effects, "confirm-shipping");
                Capture<CancelShippingCommand>(bus, effects, "cancel-shipping");
                Capture<CompleteOrderCommand>(bus, effects, "system-test-complete-order");
                bus.ReceiveEndpoint("order-submitted-faults", endpoint =>
                    endpoint.Handler<Fault<OrderSubmittedEvent>>(context =>
                    {
                        effects.RecordFault(context.Message);
                        return Task.CompletedTask;
                    }));
                bus.ConfigureEndpoints(context);
            });
        });

        var host = builder.Build();

        // Quartz keeps its logging provider in process-wide static state. System tests
        // intentionally create and dispose several hosts to model process restarts, so
        // replace the host-owned provider after construction. Otherwise a restarted
        // host can retain the previous host's disposed LoggerFactory.
        Quartz.Logging.LogProvider.SetCurrentLogProvider(NullQuartzLogProvider.Instance);
        return host;
    }

    public static async Task StopAsync(IHost host)
    {
        try
        {
            await host.StopAsync();
        }
        catch (ArgumentNullException exception) when (exception.ParamName == "source")
        {
            // Host.StopAsync enumerates its started services. When StartAsync failed
            // before that collection was assigned there is nothing to stop; disposal
            // below still releases the partially-built service provider.
        }
        finally
        {
            host.Dispose();
        }
    }

    private static void Capture<T>(
        IRabbitMqBusFactoryConfigurator bus,
        EffectProbe effects,
        string queueName)
        where T : BaseCommand
    {
        bus.ReceiveEndpoint(queueName, endpoint =>
            endpoint.Handler<T>(context =>
            {
                effects.Record(context.Message);
                return Task.CompletedTask;
            }));
    }
}

internal sealed class NullQuartzLogProvider : Quartz.Logging.ILogProvider
{
    public static NullQuartzLogProvider Instance { get; } = new();

    public Quartz.Logging.Logger GetLogger(string name) =>
        (_, _, _, _) => false;

    public IDisposable OpenMappedContext(string key, object value, bool destructure = false) =>
        NullScope.Instance;

    public IDisposable OpenNestedContext(string message) => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}

public sealed class EffectProbe
{
    private readonly ConcurrentQueue<BaseCommand> _messages = new();
    private readonly ConcurrentQueue<string> _faults = new();

    public void Record(BaseCommand message) => _messages.Enqueue(message);

    public void RecordFault<T>(Fault<T> fault)
        where T : class =>
        _faults.Enqueue(string.Join(Environment.NewLine, fault.Exceptions.Select(x => x.Message)));

    public string FaultSummary => string.Join(Environment.NewLine, _faults);

    public int Count<T>(Guid orderId, Func<T, bool>? predicate = null)
        where T : BaseCommand =>
        _messages.OfType<T>().Count(message =>
            message.OrderId == orderId && (predicate is null || predicate(message)));

    public async Task WaitForCountAsync<T>(
        Guid orderId,
        int expected,
        Func<T, bool>? predicate = null,
        TimeSpan? timeout = null)
        where T : BaseCommand
    {
        await SystemTestWait.UntilAsync(
            () => Task.FromResult(Count(orderId, predicate) >= expected),
            timeout ?? TimeSpan.FromSeconds(15));
    }
}

public static class SystemTestWait
{
    public static async Task UntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!cancellation.IsCancellationRequested)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(50, cancellation.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        Assert.True(await condition(), $"Condition was not satisfied within {timeout}.");
    }
}
