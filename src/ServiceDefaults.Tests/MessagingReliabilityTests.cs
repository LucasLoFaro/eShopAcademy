using System.Diagnostics;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServiceDefaults;

namespace ServiceDefaults.Tests;

public class MessagingReliabilityTests
{
    [Theory]
    [InlineData(typeof(TransientMessageException), MessageFailureCategory.Transient)]
    [InlineData(typeof(OrderingNotReadyException), MessageFailureCategory.OrderingNotReady)]
    [InlineData(typeof(BusinessMessageException), MessageFailureCategory.Business)]
    [InlineData(typeof(PermanentMessageException), MessageFailureCategory.Permanent)]
    public void Failure_classification_is_explicit(Type exceptionType, MessageFailureCategory expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "classified", null)!;
        Assert.Equal(expected, MessageFailureClassifier.Classify(exception));
    }

    [Fact]
    public async Task W3c_trace_context_propagates_through_MassTransit()
    {
        TraceConsumer.Reset();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(configuration => configuration.AddConsumer<TraceConsumer>())
            .BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        using var source = new ActivitySource("ServiceDefaults.Tests");
        using var producer = source.StartActivity("publish", ActivityKind.Producer)!;

        var correlationId = Guid.NewGuid();
        await harness.Bus.Publish(new TraceMessage(correlationId));
        var consumedTrace = await TraceConsumer.Trace.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(producer.TraceId, consumedTrace);
        Assert.Equal(correlationId, await TraceConsumer.Correlation.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(await harness.Consumed.Any<TraceMessage>());
        await harness.Stop();
    }

    [Fact]
    public async Task Consumer_shutdown_is_bounded_and_cancels_in_flight_work()
    {
        SlowConsumer.Reset();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddMassTransitTestHarness(configuration => configuration.AddConsumer<SlowConsumer>());
        builder.Services.Configure<MassTransitHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.ConsumerStopTimeout = TimeSpan.FromMilliseconds(100);
            options.StopTimeout = TimeSpan.FromSeconds(2);
        });
        builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(3));
        using var host = builder.Build();
        await host.StartAsync();
        await host.Services.GetRequiredService<IBus>().Publish(new SlowMessage());
        await SlowConsumer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var started = Stopwatch.GetTimestamp();
        await host.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        var elapsed = Stopwatch.GetElapsedTime(started);

        Assert.True(await SlowConsumer.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(elapsed < TimeSpan.FromSeconds(4), $"Shutdown took {elapsed}.");
    }

    [Fact]
    public void Shared_registration_sets_graceful_shutdown_budgets_and_bus_health()
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = "Testing" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Messaging:Transport"] = "RabbitMq",
            ["Messaging:RabbitMq:ConnectionString"] = "amqp://guest:guest@localhost:5672"
        });
        builder.AddServiceDefaults().WithMassTransit(messaging => messaging.UseReliabilityConventions());
        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<MassTransitHostOptions>>().Value;
        var health = host.Services.GetRequiredService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        Assert.Equal(TimeSpan.FromSeconds(50), options.ConsumerStopTimeout);
        Assert.Equal(TimeSpan.FromSeconds(60), options.StopTimeout);
        Assert.NotNull(health);
    }

    public sealed record TraceMessage(Guid CorrelationId);
    public sealed record SlowMessage;

    public sealed class TraceConsumer : IConsumer<TraceMessage>
    {
        public static TaskCompletionSource<ActivityTraceId> Trace { get; private set; } = NewTraceSource();
        public static TaskCompletionSource<Guid?> Correlation { get; private set; } = NewCorrelationSource();
        public static void Reset()
        {
            Trace = NewTraceSource();
            Correlation = NewCorrelationSource();
        }

        public Task Consume(ConsumeContext<TraceMessage> context)
        {
            Trace.TrySetResult(Activity.Current?.TraceId ?? default);
            Correlation.TrySetResult(context.CorrelationId);
            return Task.CompletedTask;
        }

        private static TaskCompletionSource<ActivityTraceId> NewTraceSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static TaskCompletionSource<Guid?> NewCorrelationSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class SlowConsumer : IConsumer<SlowMessage>
    {
        public static TaskCompletionSource<bool> Started { get; private set; } = NewSource();
        public static TaskCompletionSource<bool> Cancelled { get; private set; } = NewSource();
        public static void Reset()
        {
            Started = NewSource();
            Cancelled = NewSource();
        }

        public async Task Consume(ConsumeContext<SlowMessage> context)
        {
            Started.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                Cancelled.TrySetResult(true);
                throw;
            }
        }

        private static TaskCompletionSource<bool> NewSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
