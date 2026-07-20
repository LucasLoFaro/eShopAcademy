using System.Diagnostics.Metrics;

namespace Shipping.Application;

public static class ShippingMetrics
{
    public const string MeterName = "eShopAcademy.Shipping";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>("shipping.failures");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("consumer.retries");

    public static void RecordFailure(string operation, string reason) =>
        FailureCounter.Add(1, new("operation", operation), new("reason", reason));

    public static void RecordConsumerRetry(string consumer, string reason) =>
        RetryCounter.Add(1, new("consumer", consumer), new("reason", reason));
}
