using Microsoft.Extensions.Hosting;
using AppHost.Setup;

var builder = DistributedApplication.CreateBuilder(args);

// Basket
var basketApi = builder.AddProject("eshopacademy-basket-api", "../BasketService/API/API.csproj");
var basketEvents = builder.AddProject("eshopacademy-basket-events", "../BasketService/EventsProcessor/EventsProcessor/EventsProcessor.csproj");

// Product
var productApi = builder.AddProject("eshopacademy-product-api", "../ProductCatalogService/Presentation/API/API.csproj");
var productGrpc = builder.AddProject("eshopacademy-product-grpc", "../ProductCatalogService/Presentation/gRPC/gRPC.csproj");

// Order
var orderApi = builder.AddProject("eshopacademy-order-api", "../OrderService/Presentation/API/API.csproj");
var orderEvents = builder.AddProject("eshopacademy-order-events", "../OrderService/EventsProcessor/EventsProcessor/EventsProcessor.csproj");

// Stock
var stockApi = builder.AddProject("eshopacademy-stock-api", "../StockService/API/API.csproj");
var stockGrpc = builder.AddProject("eshopacademy-stock-grpc", "../StockService/gRPC/gRPC.csproj");

// Payment
var paymentGrpc = builder.AddProject("eshopacademy-payment-grpc", "../PaymentService/gRPC/gRPC.csproj");

// Shipping
var shippingGrpc = builder.AddProject("eshopacademy-shipping-grpc", "../ShippingService/ShippingService/ShippingService.csproj");


if (builder.Environment.IsDevelopment())
    EnvironmentSetup.SetupLocalInfrastructure(builder, basketApi, basketEvents, productApi, productGrpc, orderApi, orderEvents, stockApi, stockGrpc, paymentGrpc, shippingGrpc);

// Build and run the distributed application
builder.Build().Run();
