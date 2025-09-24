using AppHost.Setup.Extensions;
using AppHost.Setup.Resources;

namespace AppHost.Setup;

public static class EnvironmentSetup
{
    public static void SetupLocalInfrastructure(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> basketApi,
        IResourceBuilder<ProjectResource> basketEvents,
        IResourceBuilder<ProjectResource> productApi,
        IResourceBuilder<ProjectResource> productGrpc,
        IResourceBuilder<ProjectResource> orderApi,
        IResourceBuilder<ProjectResource> orderEvents,
        IResourceBuilder<ProjectResource> stockApi,
        IResourceBuilder<ProjectResource> stockGrpc,
        IResourceBuilder<ProjectResource> paymentGrpc,
        IResourceBuilder<ProjectResource> shippingGrpc)
    {
        // CHECK NAMES! containers are being called differently.

        var redis = builder.AddRedis("redis")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);

        var stockdb = builder.AddMongoDB("mongodb")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("stock");

        var ordersdb = builder.AddPostgres("postgres")
            .WithDataVolume()
            .WithPgAdmin()
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("orders");

        var cosmosdb = builder.AddCosmosDb("cosmosdb")
            .WithLifetime(ContainerLifetime.Persistent);

        var rabbit = builder.AddRabbitMQ("rabbit")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent)
            .WithManagementPlugin()     // (user/pass: guest/guest)
            .WithLifetime(ContainerLifetime.Persistent);

        // External service mocks
        var mappingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Mocks"));
        var wiremock = builder.AddContainer("external-services-mocks", "wiremock/wiremock:latest")
            .WithHttpEndpoint(port: 8090, targetPort: 8080, name: "external-services-mocks")
            .WithBindMount(mappingsPath, "/home/wiremock/mappings")
            .WithLifetime(ContainerLifetime.Persistent);

        BasketExtensions.Configure(basketApi, basketEvents, redis, rabbit);
        ProductExtensions.Configure(productApi, productGrpc, cosmosdb, rabbit);
        OrderExtensions.Configure(orderApi, orderEvents, ordersdb, rabbit);
        StockExtensions.Configure(stockApi, stockGrpc, stockdb, rabbit);
        PaymentExtensions.Configure(paymentGrpc, wiremock);
        ShippingExtensions.Configure(shippingGrpc, wiremock);
    }
}



