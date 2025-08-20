using Microsoft.Extensions.Hosting;
using AppHost.Setup;
using Aspire.Hosting.MongoDB;
using Aspire.Hosting.Redis;

var builder = DistributedApplication.CreateBuilder(args);

// Add API Gateway or top-level API if present
var api = builder.AddProject("api");

if(builder.Environment.IsDevelopment())
    SetupDependencies.AddDevelopmentEnvironment(builder, api);

// Backing services
// Add Cassandra for the Product Catalog microservice
var cassandra = builder.AddContainer("cassandra", "cassandra", "latest")
    .WithEndpoint(port: 9042, targetPort: 9042, name: "cql")
    .WithEnvironment("CASSANDRA_CLUSTER_NAME", "TestCluster")
    .WithEnvironment("CASSANDRA_START_RPC", "true");

// Add Redis for BasketService
var redis = builder.AddRedis("redis");

// Add MongoDB for StockService
var mongo = builder.AddMongoDB("mongodb")
    .AddDatabase("stock");

// Add Service Bus emulator
var serviceBus = builder.AddContainer("servicebus", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "latest")
    .WithEndpoint(port: 5672, targetPort: 5672, name: "amqp")  // AMQP
    .WithEndpoint(port: 8081, targetPort: 8081, name: "http"); // Management

// Add Postgres database for OrderService
var postgres = builder.AddPostgres("eshopacademy-ordersdb")
    .AddDatabase("orders");

// Register microservice projects with their dependencies

// BasketService API
var basketApi = builder.AddProject("basket-api", "../BasketService/API/API.csproj")
    .WithReference(redis)
    .WithReference(serviceBus);

// BasketService Events Processor
var basketEvents = builder.AddProject("basket-events", "../BasketService/EventsProcessor/EventsProcessor/EventsProcessor.csproj")
    .WithReference(redis)
    .WithReference(serviceBus);

// ProductCatalogService API
var productApi = builder.AddProject("product-api", "../ProductCatalogService/Presentation/API/API.csproj")
    .WithReference(cassandra)
    .WithReference(serviceBus);

// ProductCatalogService gRPC
var productGrpc = builder.AddProject("product-grpc", "../ProductCatalogService/Presentation/gRPC/gRPC.csproj")
    .WithReference(cassandra)
    .WithReference(serviceBus);

// OrderService API
var orderApi = builder.AddProject("order-api", "../OrderService/Presentation/API/API.csproj")
    .WithReference(postgres)
    .WithReference(serviceBus);

// OrderService Events Processor
var orderEvents = builder.AddProject("order-events", "../OrderService/EventsProcessor/EventsProcessor/EventsProcessor.csproj")
    .WithReference(postgres)
    .WithReference(serviceBus);

// StockService API
var stockApi = builder.AddProject("stock-api", "../StockService/API/API.csproj")
    .WithReference(mongo)
    .WithReference(serviceBus);

// StockService gRPC
var stockGrpc = builder.AddProject("stock-grpc", "../StockService/gRPC/gRPC.csproj")
    .WithReference(mongo)
    .WithReference(serviceBus);

// PaymentService gRPC
var paymentGrpc = builder.AddProject("payment-grpc", "../PaymentService/gRPC/gRPC.csproj");

// ShippingService gRPC
var shippingGrpc = builder.AddProject("shipping-grpc", "../ShippingService/ShippingService/ShippingService.csproj");

// Build and run the distributed application
builder.Build().Run();
