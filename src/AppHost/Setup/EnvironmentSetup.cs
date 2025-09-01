using Aspire.Hosting;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace AppHost.Setup;

public static class EnvironmentSetup
{
    public static void AddProjectDependencies(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api)
    {
        // Basket
        var basketApi = builder.AddProject("eshopacademy-basket-api", "../BasketService/API/API.csproj");
        var basketEvents = builder.AddProject("basket-events", "../BasketService/EventsProcessor/EventsProcessor/EventsProcessor.csproj");

        // Product
        var productApi = builder.AddProject("eshopacademy-product-api", "../ProductCatalogService/Presentation/API/API.csproj");
        var productGrpc = builder.AddProject("product-grpc", "../ProductCatalogService/Presentation/gRPC/gRPC.csproj");

        // Order
        var orderApi = builder.AddProject("eshopacademy-order-api", "../OrderService/Presentation/API/API.csproj");
        var orderEvents = builder.AddProject("order-events", "../OrderService/EventsProcessor/EventsProcessor/EventsProcessor.csproj");


        // Stock
        var stockApi = builder.AddProject("eshopacademy-stock-api", "../StockService/API/API.csproj");
        var stockGrpc = builder.AddProject("stock-grpc", "../StockService/gRPC/gRPC.csproj");


        // Payment
        var paymentGrpc = builder.AddProject("eshopacademy-payment-grpc", "../PaymentService/gRPC/gRPC.csproj");

        // Shipping
        var shippingGrpc = builder.AddProject("eshopacademy-shipping-grpc", "../ShippingService/ShippingService/ShippingService.csproj");


        if (builder.Environment.IsDevelopment())
        {
            // Backing services
            IResourceBuilder<RedisResource> redis;
            IResourceBuilder<MongoDBDatabaseResource> stockdb;
            IResourceBuilder<PostgresDatabaseResource> ordersdb;
            IResourceBuilder<ContainerResource> cassandra;
            IResourceBuilder<ContainerResource> serviceBus;
            IResourceBuilder<ContainerResource> wiremock;
            SetupBackingServices(builder, out redis, out stockdb, out ordersdb, out cassandra, out serviceBus, out wiremock);

            basketApi
                .WithReference(redis)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            basketEvents
                .WithReference(redis)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            productApi
                .WaitFor(serviceBus)
                .WaitFor(cassandra)
                .WithEnvironment("ConnectionStrings__Cassandra", "Contact Points=eshopacademy-cassandra;Port=9042;")
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            productGrpc
                .WaitFor(serviceBus)
                .WaitFor(cassandra)
                .WithEnvironment("ConnectionStrings__Cassandra", "Contact Points=eshopacademy-cassandra;Port=9042;")
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            orderApi
                .WithReference(ordersdb)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            orderEvents
                .WithReference(ordersdb)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            stockApi
                .WithReference(stockdb)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            stockGrpc
                .WithReference(stockdb)
                .WaitFor(serviceBus)
                .WithEnvironment("ConnectionStrings__ServiceBus",
                                 "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey");

            paymentGrpc
                .WaitFor(wiremock)
                .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080");

            shippingGrpc
                .WaitFor(wiremock)
                .WithEnvironment("WireMock__BaseUrl", "http://wiremock:8080");
        }

        static void SetupBackingServices(
            IDistributedApplicationBuilder builder, 
            out IResourceBuilder<RedisResource> redis, 
            out IResourceBuilder<MongoDBDatabaseResource> stockdb, 
            out IResourceBuilder<PostgresDatabaseResource> ordersdb, 
            out IResourceBuilder<ContainerResource> cassandra, 
            out IResourceBuilder<ContainerResource> serviceBus, 
            out IResourceBuilder<ContainerResource> wiremock)
        {
            redis = builder.AddRedis("eshopacademy-redis");
            
            stockdb = builder.AddMongoDB("eshopacademy-mongodb")
                .AddDatabase("stock");
            
            ordersdb = builder.AddPostgres("eshopacademy-postgres")
                .WithDataVolume()
                .WithPgAdmin()
                .AddDatabase("orders");

            cassandra = builder.AddContainer("eshopacademy-cassandra", "cassandra", "latest")
                .WithEndpoint(port: 9042, targetPort: 9042, name: "cql");

            //serviceBus = builder.AddAzureServiceBus("eshopacademy-servicebus");

            serviceBus = builder.AddContainer("eshopacademy-servicebus",
                                  "mcr.microsoft.com/azure-messaging/servicebus-emulator",
                                  "latest")
                    .WithEndpoint(port: 5672, targetPort: 5672, name: "amqp")
                    .WithEndpoint(port: 8081, targetPort: 8081, name: "http")
                    .WithEnvironment("SERVICEBUS_CONNECTION",
                        "Endpoint=sb://eshopacademy-servicebus/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=dummyKey")
                    .WithEnvironment("ACCEPT_EULA","Y");

            // External service mocks
            var mappingsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Mocks"));
            wiremock = builder.AddContainer("eshopacademy-external-services-mock", "wiremock/wiremock:latest")
                .WithEndpoint(8090, targetPort: 8080)
                .WithBindMount(mappingsPath, "/home/wiremock/mappings");
        }

        //api.WithReference(wiremock);

        // 🔹 RabbitMQ
        //var rabbit = builder.AddRabbitMQ("rabbit")
        //    .WithEndpoint(5672)
        //    .WithManagementPlugin(); // acceso en http://localhost:15672

        //api.WithReference(rabbit);

        // Cassandra has been moved into the AppHost so that it can be
        // referenced by multiple microservices alongside other backing
        // resources (Redis, MongoDB and PostgreSQL). Only the WireMock
        // external service mock is configured here for development.
    }
}
