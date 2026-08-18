using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using ServiceDefaults;
using Stock.Messaging.Processor.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(StockMetrics.MeterName));

builder.AddServiceDefaults()
    .AddRequiredConnectionString("stock")
    .WithMassTransit(messaging =>
    {
        messaging.UseReliabilityConventions();
        messaging.ReceiveEndpoint<CommitStockReservationConsumer>("commit-stock-reservation");
        messaging.ReceiveEndpoint<ReleaseStockReservationConsumer>("release-stock-reservation");
        messaging.ReceiveEndpoint<ProductPublishedConsumer>("product-published-stock");

    }, typeof(CommitStockReservationConsumer).Assembly)
    .AddWorkerHealthEndpoints();

builder.Services.AddSingleton(sp => new StockDbContext(
    sp.GetRequiredService<IOptionsMonitor<RequiredConnectionString>>().Get("stock").Value,
    "stock"));
builder.Services.AddSingleton<IMongoClient>(sp => sp.GetRequiredService<StockDbContext>().Client);
builder.Services.AddSingleton(sp => sp.GetRequiredService<StockDbContext>().Database);
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();
builder.Services.AddHealthChecks().AddCriticalDependency(
    "stock-mongodb",
    async (sp, cancellationToken) =>
        await sp.GetRequiredService<StockDbContext>().PingAsync(cancellationToken));

var host = builder.Build();
host.Run();
