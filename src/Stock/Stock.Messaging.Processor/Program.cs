using Infrastructure.Data;
using Infrastructure.Services;
using ServiceDefaults;
using Stock.Messaging.Processor.Consumers;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

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
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();
builder.Services.AddHealthChecks().AddCriticalDependency(
    "stock-mongodb",
    async (sp, cancellationToken) =>
        await sp.GetRequiredService<StockDbContext>().PingAsync(cancellationToken));

var host = builder.Build();
host.Run();
