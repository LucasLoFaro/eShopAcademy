using System.Diagnostics.Metrics;

namespace Infrastructure.Observability;

public static class PaymentTelemetry
{
    public const string MeterName = "eShopAcademy.Payments";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Results = Meter.CreateCounter<long>("payments.results");
    private static readonly Counter<long> Retries = Meter.CreateCounter<long>("payments.retries");
    private static readonly Counter<long> Faults = Meter.CreateCounter<long>("payments.message_faults");

    public static void RecordResult(string operation, string result) =>
        Results.Add(1, new("operation", operation), new("result", result));

    public static void RecordRetry(string operation) =>
        Retries.Add(1, new KeyValuePair<string, object?>("operation", operation));

    public static void RecordFault(string processor, string category) =>
        Faults.Add(1, new("processor", processor), new("category", category));
}
