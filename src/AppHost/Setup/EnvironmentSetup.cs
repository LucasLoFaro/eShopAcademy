using Microsoft.Extensions.DependencyInjection;
using AppHost.Setup.Extensions;


namespace AppHost.Setup;

public static class EnvironmentSetup
{
    public static void SetupLocalInfrastructure(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> basketApi,
        IResourceBuilder<ProjectResource> basketEvents,
        IResourceBuilder<ProjectResource> productsApi,
        IResourceBuilder<ProjectResource> productsGrpc,
        IResourceBuilder<ProjectResource> ordersApi,
        IResourceBuilder<ProjectResource> ordersOrchestration,
        IResourceBuilder<ProjectResource> ordersMessaging,
        IResourceBuilder<ProjectResource> stockApi,
        IResourceBuilder<ProjectResource> stockGrpc,
        IResourceBuilder<ProjectResource> stockMessaging,
        IResourceBuilder<ProjectResource> paymentsApi,
        IResourceBuilder<ProjectResource> paymentsGrpc,
        IResourceBuilder<ProjectResource> paymentsMessaging,
        IResourceBuilder<ProjectResource> shippingApi,
        IResourceBuilder<ProjectResource> shippingService,
        IResourceBuilder<ProjectResource> notificationService,
        IResourceBuilder<ProjectResource> customersApi,
        IResourceBuilder<ProjectResource> operationsApi,
        IResourceBuilder<ProjectResource> operationsService)
    {
        var redis = builder.AddRedis("redis")
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent);

        var mongo = builder.AddMongoDB("mongodb")
            .WithDataVolume("mongo-data")
            .WithLifetime(ContainerLifetime.Persistent);

        var stockdb = mongo.AddDatabase("stock");
        var customersdb = mongo.AddDatabase("customers");
        var shippingdb = mongo.AddDatabase("shipping");
        var operationsdb = mongo.AddDatabase("operations");

        var postgres = builder.AddPostgres("postgres")
            .WithDataVolume("postgres-data")
            .WithLifetime(ContainerLifetime.Persistent);

        var ordersdb = postgres.AddDatabase("orders");
        var orchestrationdb = postgres.AddDatabase("orchestration");        

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
        ProductsExtensions.Configure(productsApi, productsGrpc, cosmosdb, rabbit);
        OrdersExtensions.Configure(ordersApi, ordersOrchestration, ordersMessaging, ordersdb, orchestrationdb, rabbit);
        StockExtensions.Configure(stockApi, stockGrpc, stockMessaging, stockdb, rabbit);
        PaymentsExtensions.Configure(paymentsApi, paymentsGrpc, paymentsMessaging, wiremock, rabbit);
        ShippingExtensions.Configure(shippingApi, shippingService, wiremock, rabbit, shippingdb);
        NotificationExtensions.Configure(notificationService, rabbit);
        CustomersExtensions.Configure(customersApi, customersdb, rabbit);
        OperationsExtensions.Configure(operationsApi, operationsService, operationsdb, rabbit);
    }
}



