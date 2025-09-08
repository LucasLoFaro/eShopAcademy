using StackExchange.Redis;

namespace AppHost.Setup.Extensions;

public static class OrderExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> orderApi,
        IResourceBuilder<ProjectResource> orderEvents,
        IResourceBuilder<PostgresDatabaseResource> ordersdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        orderApi
            .WithReference(ordersdb)
            .WaitFor(ordersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(8003)
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");

        orderEvents
            .WithReference(ordersdb)
            .WaitFor(ordersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");
    }
}