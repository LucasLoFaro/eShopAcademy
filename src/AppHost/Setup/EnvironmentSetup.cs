using AppHost.Setup.Extensions;

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
        var redis = builder.AddRedis("redis");

        var stockdb = builder.AddMongoDB("mongodb")
            .AddDatabase("stock");

        var ordersdb = builder.AddPostgres("postgres")
            .WithDataVolume()
            .WithPgAdmin()
            .AddDatabase("orders");

        var cassandra = builder.AddContainer("cassandra", "cassandra", "latest")
            .WithEndpoint(port: 9042, targetPort: 9042, name: "cql");

        var rabbit = builder.AddRabbitMQ("rabbit")
            .WithDataVolume()
            .WithManagementPlugin(); // (user/pass: guest/guest)

        // External service mocks
        var mappingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Mocks"));
        var wiremock = builder.AddContainer("eshopacademy-external-services-mocks", "wiremock/wiremock:latest")
            .WithEndpoint(8090, targetPort: 8080)
            .WithBindMount(mappingsPath, "/home/wiremock/mappings");

        BasketExtensions.Configure(basketApi, basketEvents, redis, rabbit);
        ProductExtensions.Configure(productApi, productGrpc, cassandra, rabbit);
        OrderExtensions.Configure(orderApi, orderEvents, ordersdb, rabbit);
        StockExtensions.Configure(stockApi, stockGrpc, stockdb, rabbit);
        PaymentExtensions.Configure(paymentGrpc, wiremock);
        ShippingExtensions.Configure(shippingGrpc, wiremock);
    }
}



