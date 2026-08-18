using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ServiceDefaults;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Application.Options;
using Shipping.Service.Consumers;
using Shipping.Application;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(ShippingMetrics.MeterName));

var connectionString = builder.Configuration.GetConnectionString("shipping");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The shipping MongoDB connection string is not configured.");

var shippingDatabase = new ShippingDbContext(
    connectionString,
    builder.Configuration["Shipping:Database"] ?? "shipping");
builder.Services.AddSingleton(shippingDatabase);
builder.Services.AddSingleton<IMongoClient>(shippingDatabase.Client);
builder.Services.AddSingleton(shippingDatabase.Database);
builder.Services.AddScoped<IShippingInfoRepository, ShippingInfoRepository>();
builder.Services.AddScoped<IShippingOperationStore, ShippingOperationStore>();

builder.Services.AddOptions<ShippingProviderOptions>()
    .Bind(builder.Configuration.GetSection("Shipping:Provider"))
    .Validate(ShippingProviderOptions.IsValid, "Shipping provider BaseUrl must be an absolute HTTP(S) URI.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IShippingProviderClient, ShippingProviderClient>((sp, client) =>
{
    client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<ShippingProviderOptions>>().Value.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.AddServiceDefaults()
    .WithMassTransit(assemblies: typeof(ScheduleShippingCommandConsumer).Assembly);
builder.AddRequiredSecret("Shipping:SignatureSecret");

builder.Services.AddHealthChecks().AddAsyncCheck(
    "shipping-database",
    async cancellationToken =>
    {
        await shippingDatabase.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();
