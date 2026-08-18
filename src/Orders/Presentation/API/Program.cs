using System.Reflection;
using API.Sse;
using Application;
using Application.Observability;
using Core.Application.Interfaces;
using Infrastructure.Clients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Protos;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithSwagger()
    .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(OrdersTelemetry.MeterName));
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0)
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddSingleton<OrderStatusStreamService>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(client =>
    client.BaseAddress = RequiredServiceUri(
        builder.Configuration,
        "services:eshopacademy-customers-api:customers-api:0"));
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
    client.BaseAddress = RequiredServiceUri(
        builder.Configuration,
        "services:eshopacademy-products-api:products-api:0"));

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

#pragma warning disable EXTEXP0001
builder.Services.AddGrpcClient<PaymentGrpc.PaymentGrpcClient>(options =>
{
    options.Address = RequiredServiceUri(
        builder.Configuration,
        "services:eshopacademy-payments-grpc:payments-grpc:0");
}).RemoveAllResilienceHandlers();
builder.Services.AddGrpcClient<StockProtoService.StockProtoServiceClient>(options =>
{
    options.Address = RequiredServiceUri(
        builder.Configuration,
        "services:eshopacademy-stock-grpc:stock-grpc:0");
}).RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase("OrdersTests"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("orders");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("ConnectionStrings:orders is required.");

    builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));
}

builder.Services.AddHealthChecks().AddCheck<OrderDatabaseHealthCheck>(
    "orders-db",
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IStockServiceClient, StockServiceClient>();
builder.Services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();
builder.Services.AddScoped<IOrderMessagingClient, OrderMessagingClient>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.MapControllers();
app.UseDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    if (database.Database.IsRelational())
        database.Database.Migrate();
}

app.Run();

static Uri RequiredServiceUri(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        throw new InvalidOperationException($"Service base address configuration '{key}' must be an absolute HTTP(S) URI.");

    return uri;
}

public partial class Program { }
