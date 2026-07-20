using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using ServiceDefaults;
using Stock.Messaging.Processor.Consumers;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("stock");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The stock MongoDB connection string is not configured.");

var stockDatabase = new StockDbContext(connectionString, "stock");
builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(StockMetrics.MeterName));
builder.Services.AddSingleton(stockDatabase);
builder.Services.AddSingleton<IMongoClient>(stockDatabase.Client);
builder.Services.AddSingleton(stockDatabase.Database);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration => registration.AddMongoDbOutbox(options =>
        {
            options.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
            options.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
            options.QueryDelay = TimeSpan.FromSeconds(1);
            options.DuplicateDetectionWindow = TimeSpan.FromHours(1);
        }));
    }, typeof(CommitStockReservationConsumer).Assembly);

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

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();
