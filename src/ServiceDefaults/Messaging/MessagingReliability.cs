using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceDefaults;

public enum MessageFailureCategory
{
    Transient,
    OrderingNotReady,
    Business,
    Permanent,
    Unclassified
}

public class TransientMessageException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class OrderingNotReadyException(string message, Exception? innerException = null)
    : TransientMessageException(message, innerException);

public sealed class BusinessMessageException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class PermanentMessageException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public static class MessageFailureClassifier
{
    public static MessageFailureCategory Classify(Exception exception) => exception switch
    {
        OrderingNotReadyException => MessageFailureCategory.OrderingNotReady,
        TransientMessageException or TimeoutException => MessageFailureCategory.Transient,
        BusinessMessageException => MessageFailureCategory.Business,
        PermanentMessageException or ArgumentException or FormatException or UnauthorizedAccessException =>
            MessageFailureCategory.Permanent,
        _ => MessageFailureCategory.Unclassified
    };
}

public sealed class MessagingReliabilityOptions
{
    public const string SectionName = "Messaging:Reliability";

    public int ImmediateRetryCount { get; set; } = 2;
    public TimeSpan[] ImmediateRetryIntervals { get; set; } =
        [TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500)];
    public TimeSpan[] RedeliveryIntervals { get; set; } =
        [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)];
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan ConsumerStopTimeout { get; set; } = TimeSpan.FromSeconds(50);

    internal static MessagingReliabilityOptions Resolve(IConfiguration configuration)
    {
        var options = configuration.GetSection(SectionName).Get<MessagingReliabilityOptions>() ?? new();
        if (options.ImmediateRetryCount is < 0 or > 2 ||
            options.ImmediateRetryIntervals.Length != options.ImmediateRetryCount ||
            options.ImmediateRetryIntervals.Any(interval => interval <= TimeSpan.Zero) ||
            options.RedeliveryIntervals is not { Length: > 0 and <= 3 } ||
            options.RedeliveryIntervals.Any(interval => interval <= TimeSpan.Zero) ||
            options.ConsumerStopTimeout <= TimeSpan.Zero ||
            options.ShutdownTimeout <= options.ConsumerStopTimeout)
        {
            throw new OptionsValidationException(
                SectionName,
                typeof(MessagingReliabilityOptions),
                ["Messaging:Reliability must use at most two immediate retries, at most three positive redelivery intervals, and a consumer stop timeout shorter than the host shutdown timeout."]);
        }

        return options;
    }
}

internal static class MessagingReliabilityPolicy
{
    public static void Apply(IReceiveEndpointConfigurator endpoint, MessagingReliabilityOptions options)
    {
        endpoint.UseDelayedRedelivery(redelivery =>
        {
            redelivery.Handle<TransientMessageException>();
            redelivery.Handle<TimeoutException>();
            redelivery.Handle<OrderingNotReadyException>();
            redelivery.Intervals(options.RedeliveryIntervals);
        });
        endpoint.UseMessageRetry(retry =>
        {
            retry.Handle<TransientMessageException>();
            retry.Handle<TimeoutException>();
            retry.Ignore<OrderingNotReadyException>();
            retry.Intervals(options.ImmediateRetryIntervals);
        });
    }

    public static void ConfigureHost(IServiceCollection services, MessagingReliabilityOptions options)
    {
        services.Configure<MassTransitHostOptions>(host =>
        {
            host.WaitUntilStarted = true;
            host.StartTimeout = TimeSpan.FromSeconds(30);
            host.StopTimeout = options.ShutdownTimeout;
            host.ConsumerStopTimeout = options.ConsumerStopTimeout;
        });
        services.Configure<HostOptions>(host => host.ShutdownTimeout = options.ShutdownTimeout);
    }
}
