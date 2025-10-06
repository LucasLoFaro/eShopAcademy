using Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
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
    options.Address = new Uri(builder.Configuration["services:eshopacademy-payment-grpc:payment-grpc:0"]!);
});


builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("orders")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockServiceClient, FakeStockServiceClient>();
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
    db.Database.Migrate();
}

app.Run();

public partial class Program { }