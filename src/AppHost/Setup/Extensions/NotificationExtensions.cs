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
            .WithEnvironment("SendGrid__ApiKey", sendGridApiKey);

        notificationApi
            .WithReference(notificationsdb)
            .WaitFor(notificationsdb)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8011, name: "notification-api");
    }
}
