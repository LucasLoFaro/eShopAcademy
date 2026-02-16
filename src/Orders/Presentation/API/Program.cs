using Application;
using Core.Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Clients;
using Microsoft.EntityFrameworkCore;
using Protos;
using ServiceDefaults;
using API.Sse;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger()
       .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

builder.Services.AddSingleton<OrderStatusStreamService>();

builder.Services.AddControllers()
    .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(client =>
{
    var baseAddress = builder.Configuration["services:eshopacademy-customers-api:customers-api:0"];

    if (string.IsNullOrWhiteSpace(baseAddress))
    {
        throw new InvalidOperationException(
            "Customers API base address configuration 'services:eshopacademy-customers-api:customers-api:0' is missing.");
    }

    client.BaseAddress = new Uri(baseAddress);
});

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    var baseAddress = builder.Configuration["services:eshopacademy-products-api:products-api:0"];

    if (string.IsNullOrWhiteSpace(baseAddress))
    {
        throw new InvalidOperationException(
            "Products API base address configuration 'services:eshopacademy-products-api:products-api:0' is missing.");
    }

    client.BaseAddress = new Uri(baseAddress);
});

builder.Services.AddGrpcClient<PaymentGrpc.PaymentGrpcClient>(options =>
{
    options.Address = new Uri(builder.Configuration["services:eshopacademy-payments-grpc:payments-grpc:0"]!);
});
builder.Services.AddGrpcClient<StockProtoService.StockProtoServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["services:eshopacademy-stock-grpc:stock-grpc:0"]!);
});

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseInMemoryDatabase("OrdersTests"));
}
else
{
    builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("orders")));
}
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockServiceClient, StockServiceClient>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>(); 
builder.Services.AddScoped<IOrderMessagingClient, OrderMessagingClient>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
app.MapControllers();
app.UseDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
}

app.Run();

public partial class Program { }