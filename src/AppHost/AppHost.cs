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

// Sellers
var sellersApi = builder.AddProject<Projects.Sellers_Api>("eshopacademy-sellers-api");
var sellersService = builder.AddProject<Projects.Sellers_Service>("eshopacademy-sellers-service");
var sellersEventsProcessor = builder.AddProject<Projects.Sellers_EventsProcessor>("eshopacademy-sellers-events");

// Shipping
var shippingApi = builder.AddProject<Projects.Shipping_Api>("eshopacademy-shipping-api");
var shippingService = builder.AddProject<Projects.Shipping_Service>("eshopacademy-shipping-service");

// Customer
var customersApi = builder.AddProject<Projects.Customers_Api>("eshopacademy-customers-api");
var customersMessaging = builder.AddProject<Projects.Customers_Messaging>("eshopacademy-customers-messaging");

// SAGA Orchestration
var ordersOrchestration = builder.AddProject<Projects.Orchestration>("eshopacademy-orchestration");

// Order
var ordersApi = builder.AddProject<Projects.Orders_API>("eshopacademy-orders-api")
                        .WithReference(customersApi)
                        .WithReference(productsApi)
                        .WithReference(productsGrpc)
                        .WithReference(stockGrpc)
                        .WithReference(paymentsGrpc);
var ordersMessaging = builder.AddProject<Projects.Orders_Messaging>("eshopacademy-orders-messaging");

// Notification
var notificationService = builder.AddProject<Projects.Notification_Service>("eshopacademy-notification-service");
var notificationApi = builder.AddProject<Projects.Notification_Api>("eshopacademy-notification-api");

// API Gateway
var gateway = builder.AddProject<Projects.Gateway>("eshopacademy-gateway")
                     .WithReference(productsApi)
                     .WithReference(basketApi)
                     .WithReference(ordersApi)
                     .WithReference(customersApi)
                     .WithReference(paymentsApi)
                     .WithReference(shippingApi)
                     .WithReference(operationsApi)
                     .WithReference(notificationApi)
                     .WithReference(sellersApi);

// Consumer Frontend (React + Vite)
var frontend = builder.AddViteApp("eshopacademy-frontend", "../Frontend/eshop-web")
                      .WithEndpoint("http", e => e.Port = 5173)
                      .WithEnvironment("VITE_GATEWAY_URL", gateway.GetEndpoint("gateway"));

// Sellers Frontend Microfrontend (React + Vite)
var sellersFrontend = builder.AddViteApp("eshopacademy-sellers-frontend", "../Sellers/Frontend")
                             .WithEndpoint("http", e => e.Port = 5174)
                             .WithEnvironment("VITE_SELLERS_API_BASE_URL", sellersApi.GetEndpoint("sellers-api"))
                             .WithEnvironment("VITE_DEFAULT_SELLER_ID", "00000000-0000-0000-0000-000000000000");


if (builder.Environment.IsDevelopment())
EnvironmentSetup.SetupLocalInfrastructure(builder, basketApi, basketEvents, productsApi, productsGrpc, ordersApi, ordersOrchestration, ordersMessaging, stockApi, stockGrpc, stockMessaging, paymentsApi, paymentsGrpc, paymentsMessaging, shippingApi, shippingService, notificationService, notificationApi, customersApi, customersMessaging, operationsApi, operationsService, sellersApi, sellersService, sellersEventsProcessor, gateway);


// Build and run the distributed application
builder.Build().Run();
