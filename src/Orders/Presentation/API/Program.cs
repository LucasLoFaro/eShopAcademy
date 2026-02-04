using Microsoft.EntityFrameworkCore;
using Core.Application.Interfaces;
using Infrastructure.Services;
using Infrastructure.Clients;
using ServiceDefaults;
using Application;
using Protos;
using Data;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger()
       .WithMassTransit();

builder.Services.AddControllers()
    .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

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
builder.Services.AddScoped<ICustomerServiceClient, FakeCustomerServiceClient>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>(); 
builder.Services.AddScoped<IProductServiceClient, FakeProductServiceClient>();
builder.Services.AddScoped<IOrderMessagingClient, OrderMessagingClient>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();
app.MapControllers();
app.UseDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
}

app.Run();

public partial class Program { }