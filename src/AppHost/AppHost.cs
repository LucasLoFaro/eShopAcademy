using Microsoft.Extensions.Hosting;
using AppHost.Setup;

var builder = DistributedApplication.CreateBuilder(args);

// Basket
var basketApi = builder.AddProject<Projects.Basket_API>("eshopacademy-basket-api");
var basketEvents = builder.AddProject<Projects.Basket_EventsProcessor>("eshopacademy-basket-events");

// Product
var productApi = builder.AddProject<Projects.Product_API>("eshopacademy-product-api");
var productGrpc = builder.AddProject<Projects.Product_gRPC> ("eshopacademy-product-grpc");

// Order
var orderApi = builder.AddProject<Projects.Order_API>("eshopacademy-order-api");
var orderEvents = builder.AddProject<Projects.Order_EventsProcessor>("eshopacademy-order-events");

// Stock
var stockApi = builder.AddProject<Projects.Stock_API>("eshopacademy-stock-api");
var stockGrpc = builder.AddProject<Projects.Stock_gRPC>("eshopacademy-stock-grpc");

// Payment
var paymentGrpc = builder.AddProject<Projects.Payment_gRPC>("eshopacademy-payment-grpc");

// Shipping
var shippingGrpc = builder.AddProject<Projects.Shipping_gRPC>("eshopacademy-shipping-grpc");


if (builder.Environment.IsDevelopment())
    EnvironmentSetup.SetupLocalInfrastructure(builder, basketApi, basketEvents, productApi, productGrpc, orderApi, orderEvents, stockApi, stockGrpc, paymentGrpc, shippingGrpc);

// Build and run the distributed application
builder.Build().Run();
