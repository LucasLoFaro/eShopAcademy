using gRPC.Services;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceDefaults;
using OpenTelemetry.Metrics;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit();

builder.Services.AddGrpc();
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(StockMetrics.MeterName));
var connectionString = builder.Configuration.GetConnectionString("stock");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The stock MongoDB connection string is not configured.");

var stockDatabase = new StockDbContext(connectionString, "stock");
builder.Services.AddSingleton(stockDatabase);
builder.Services.AddHealthChecks().AddAsyncCheck(
    "stock-database",
    async cancellationToken =>
    {
        await stockDatabase.PingAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    },
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();


builder.WebHost.ConfigureKestrel(o =>
{
    o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
});

var app = builder.Build();

app.MapGrpcService<StockService>();
app.MapGet("/", () => "Mock endpoint");
app.UseDefaultEndpoints();

app.Run();
