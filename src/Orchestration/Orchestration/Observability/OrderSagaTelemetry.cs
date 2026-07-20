using System.Diagnostics.Metrics;

namespace Orchestration.Observability;

public static class OrderSagaTelemetry
{
    public const string MeterName = "eShopAcademy.OrderSaga";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("order_saga.failures");
    private static readonly Counter<long> Retries = Meter.CreateCounter<long>("order_saga.retries");
    private static readonly Counter<long> Faults = Meter.CreateCounter<long>("order_saga.message_faults");

    public static void RecordFailure(string category) =>
        Failures.Add(1, new KeyValuePair<string, object?>("category", category));

    public static void RecordRetry(string category) =>
        Retries.Add(1, new KeyValuePair<string, object?>("category", category));

    public static void RecordFault(string category) =>
        Faults.Add(1, new KeyValuePair<string, object?>("category", category));
}
