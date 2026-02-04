using Microsoft.Extensions.Hosting;
using AppHost.Setup;

var builder = DistributedApplication.CreateBuilder(args);

// Basket
var basketApi = builder.AddProject<Projects.Basket_API>("eshopacademy-basket-api");
var basketEvents = builder.AddProject<Projects.Basket_EventsProcessor>("eshopacademy-basket-events");

// Product
var productsApi = builder.AddProject<Projects.Products_API>("eshopacademy-products-api");
var productsGrpc = builder.AddProject<Projects.Products_gRPC>("eshopacademy-products-grpc");

// Stock
var stockApi = builder.AddProject<Projects.Stock_API>("eshopacademy-stock-api");
var stockGrpc = builder.AddProject<Projects.Stock_gRPC>("eshopacademy-stock-grpc");
var stockMessaging = builder.AddProject<Projects.Stock_Messaging_Processor>("eshopacademy-stock-messaging");

// Payment
var paymentsApi = builder.AddProject<Projects.Payments_API>("eshopacademy-payments-api");
var paymentsGrpc = builder.AddProject<Projects.Payments_gRPC>("eshopacademy-payments-grpc");
var paymentsMessaging = builder.AddProject<Projects.Payments_Messaging>("eshopacademy-payments-messaging");

// Operations
var operationsApi = builder.AddProject<Projects.Operations_Api>("eshopacademy-operations-api");
var operationsService = builder.AddProject<Projects.Operations_Service>("eshopacademy-operations-service");

// Shipping
var shippingApi = builder.AddProject<Projects.Shipping_Api>("eshopacademy-shipping-api");
var shippingService = builder.AddProject<Projects.Shipping_Service>("eshopacademy-shipping-service");

// Customer
var customersApi = builder.AddProject<Projects.Customers_Api>("eshopacademy-customers-api");

// Order
var ordersOrchestration = builder.AddProject<Projects.Orders_Orchestration>("eshopacademy-orders-orchestration");
var ordersApi = builder.AddProject<Projects.Orders_API>("eshopacademy-orders-api")
                        .WithReference(customersApi)
                        .WithReference(productsGrpc)
                        .WithReference(stockGrpc)
                        .WithReference(paymentsGrpc);

// Notification
var notificationService = builder.AddProject<Projects.Notification_Service>("eshopacademy-notification-service");


if (builder.Environment.IsDevelopment())
    EnvironmentSetup.SetupLocalInfrastructure(builder, basketApi, basketEvents, productsApi, productsGrpc, ordersApi, ordersOrchestration, stockApi, stockGrpc, stockMessaging, paymentsApi, paymentsGrpc, paymentsMessaging, shippingApi, shippingService, notificationService, customersApi, operationsApi, operationsService);


// Build and run the distributed application
builder.Build().Run();
