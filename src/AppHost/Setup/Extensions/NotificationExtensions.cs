namespace AppHost.Setup.Extensions;

public static class NotificationExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> notificationService,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        notificationService
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            // Change Azure identity and add SendGrid api key to key vault.
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
    }
}
