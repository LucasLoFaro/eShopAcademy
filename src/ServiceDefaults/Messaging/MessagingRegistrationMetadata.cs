namespace ServiceDefaults;

public enum MessagingScheduler
{
    None,
    Quartz,
    AzureServiceBusNative
}

public sealed record MessagingRegistrationMetadata(
    MessagingTransport Transport,
    MessagingScheduler Scheduler,
    bool RuntimeTopologyCreation);
