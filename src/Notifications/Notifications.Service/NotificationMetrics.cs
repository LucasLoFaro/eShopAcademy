using System.Diagnostics.Metrics;

namespace NotificationService;

public static class NotificationMetrics
{
    public const string MeterName = "eShopAcademy.Notifications";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>("notification.failures");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("consumer.retries");

    public static void RecordFailure(string channel, string reason) =>
        FailureCounter.Add(1, new("channel", channel), new("reason", reason));

    public static void RecordConsumerRetry(string consumer, string reason) =>
        RetryCounter.Add(1, new("consumer", consumer), new("reason", reason));
}
