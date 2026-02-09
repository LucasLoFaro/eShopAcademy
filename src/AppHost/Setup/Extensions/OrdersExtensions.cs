using AppHost.Setup.Resources;
using Aspire.Hosting;

namespace AppHost.Setup.Extensions;

public static class OrdersExtensions
{
    public static void Configure(
        IResourceBuilder<ProjectResource> orderApi,
        IResourceBuilder<ProjectResource> orderOrchestration,
        IResourceBuilder<ProjectResource> ordersMessaging,
        IResourceBuilder<PostgresDatabaseResource> ordersdb,
        IResourceBuilder<PostgresDatabaseResource> orchestrationdb,
        IResourceBuilder<RabbitMQServerResource> rabbit)
    {
        orderApi
            .WithReference(ordersdb)
            .WaitFor(ordersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithHttpEndpoint(port: 8003, name: "orders-api")
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");

        orderOrchestration
            .WithReference(orchestrationdb)
            .WaitFor(orchestrationdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");

        ordersMessaging
            .WithReference(ordersdb)
            .WaitFor(ordersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments()
            .WithEnvironment("AZURE_CLIENT_ID", "31234358-e1e6-40e9-82fe-363a97801e4e")
            .WithEnvironment("AZURE_CLIENT_SECRET", "piI8Q~iaVPv30ukYTzBXKUi4FTaxoxglI4.Buapp");
    }
}
