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
            .WithHttpEndpoint(port: 8003, name: "orders-api");

        orderOrchestration
            .WithReference(orchestrationdb)
            .WaitFor(orchestrationdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();

        ordersMessaging
            .WithReference(ordersdb)
            .WaitFor(ordersdb)
            .WithReference(rabbit)
            .WaitFor(rabbit)
            .WithCommonEnvironments();
    }
}
