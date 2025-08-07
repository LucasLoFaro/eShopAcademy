namespace Infrastructure.Services.Settings;

/// <summary>
/// Represents configuration required to connect to Azure Service Bus.  The
/// Host property corresponds to the fully qualified namespace of the
/// Service Bus (e.g. my-service-bus.servicebus.windows.net) without
/// protocol.  Additional settings can be added as needed.
/// </summary>
public class ServiceBusSettings
{
    public string Host { get; set; } = string.Empty;
}