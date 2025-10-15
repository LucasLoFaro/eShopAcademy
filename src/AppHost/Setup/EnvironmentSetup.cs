using AppHost.Setup.Extensions;
using Aspire.Hosting;
using Microsoft.Extensions.DependencyInjection;

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
        IResourceBuilder<ProjectResource> orderOrchestration,
        IResourceBuilder<ProjectResource> stockApi,
        IResourceBuilder<ProjectResource> stockGrpc,
        IResourceBuilder<ProjectResource> paymentApi,
        IResourceBuilder<ProjectResource> paymentGrpc,
        IResourceBuilder<ProjectResource> shippingApi,
        IResourceBuilder<ProjectResource> shippingGrpc,
        IResourceBuilder<ProjectResource> notificationService)
    {
        var redis = builder.AddRedis("redis")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);

        var stockdb = builder.AddMongoDB("mongodb")
            .WithDataVolume("mongo-data")
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase("stock");

        var postgres = builder.AddPostgres("postgres")
            .WithDataVolume("postgres-data")
            .WithLifetime(ContainerLifetime.Persistent);

        var ordersdb = postgres.AddDatabase("orders");

        postgres.WithPgAdmin()
            .WithLifetime(ContainerLifetime.Persistent);
        
        var cosmosdb = builder.AddCosmosDb("cosmosdb")
            .WithLifetime(ContainerLifetime.Persistent);

        var rabbit = builder.AddRabbitMQ("rabbit")
            .WithDataVolume("rabbit-data")
            .WithLifetime(ContainerLifetime.Persistent)
            .WithManagementPlugin()     // (user/pass: guest/guest)
            .WithLifetime(ContainerLifetime.Persistent);

        var wiremock = builder.AddWiremock("external-services-mocks")
            .WithHttpEndpoint(port: 8090, targetPort: 8080, name: "external-services-mocks")
            .WithLifetime(ContainerLifetime.Persistent);

        BasketExtensions.Configure(basketApi, basketEvents, redis, rabbit);
        ProductExtensions.Configure(productApi, productGrpc, cosmosdb, rabbit);
        OrderExtensions.Configure(orderApi, orderOrchestration, ordersdb, rabbit);
        StockExtensions.Configure(stockApi, stockGrpc, stockdb, rabbit);
        PaymentExtensions.Configure(paymentApi, paymentGrpc, wiremock, rabbit);
        ShippingExtensions.Configure(shippingApi, shippingGrpc, wiremock, rabbit);
        NotificationExtensions.Configure(notificationService, rabbit);
    }
}



