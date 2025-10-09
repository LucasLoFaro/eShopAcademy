using System.Reflection;

namespace Domain.Events;

public class BaseEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; init; } = Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown";
    public string Source { get; init; } = Environment.GetEnvironmentVariable("DOTNET_RESOURCE_NAME") ?? "Unknown";
    public string Version { get; init; } = Assembly.GetEntryAssembly()?
                                                   .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                                   .InformationalVersion ?? "Unknown";
}