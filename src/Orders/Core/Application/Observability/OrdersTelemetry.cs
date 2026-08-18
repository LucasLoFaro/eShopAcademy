using System.Diagnostics.Metrics;

namespace Application.Observability;

public static class OrdersTelemetry
{
    public const string MeterName = "eShopAcademy.Orders";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> MessageFaults = Meter.CreateCounter<long>("orders.message_faults");
    private static readonly Counter<long> Results = Meter.CreateCounter<long>("orders.message_results");

    public static void RecordMessageFault(string processor, string category) =>
        MessageFaults.Add(1, new("processor", processor), new("category", category));

    public static void RecordResult(string processor, string result) =>
        Results.Add(1, new("processor", processor), new("result", result));
}
