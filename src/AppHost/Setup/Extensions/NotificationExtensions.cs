namespace AppHost.Setup.Extensions;

public static class NotificationExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> notificationService,
        IResourceBuilder<ProjectResource> notificationApi,
        IResourceBuilder<MongoDBDatabaseResource> notificationsdb,
        IResourceBuilder<RabbitMQServerResource> rabbit,
        IResourceBuilder<ParameterResource> sendGridApiKey)
    {
        notificationService
            .WithReference(notificationsdb)
            .WaitFor(notificationsdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("SendGrid__ApiKey", sendGridApiKey)
            // Change Azure identity and add SendGrid api key to key vault.
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");

        notificationApi
            .WithReference(notificationsdb)
            .WaitFor(notificationsdb)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8010, name: "notification-api")
            .WithEnvironment("AZURE_CLIENT_ID", "bdcf29bc-ebed-428f-9b86-e3d84d9a2314")
            .WithEnvironment("AZURE_CLIENT_SECRET", "ceL8Q~xXDZOCEjzSCB16Gv-cpDQxy7iKNcT6-cxX");
    }
}
