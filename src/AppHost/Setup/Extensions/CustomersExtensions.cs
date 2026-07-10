namespace AppHost.Setup.Extensions;

public static class CustomersExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> customerApi,
        IResourceBuilder<MongoDBDatabaseResource> customersdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        customerApi
            .WithReference(customersdb)
            .WaitFor(customersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8008, name: "customers-api");
    }

    public static void ConfigureMessaging(
        IResourceBuilder<ProjectResource> customersMessaging,
        IResourceBuilder<MongoDBDatabaseResource> customersdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        customersMessaging
            .WithReference(customersdb)
            .WaitFor(customersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}