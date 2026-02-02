using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using ServiceDefaults;
using Stock.Messaging.Processor.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("commit-stock-reservation", e =>
            e.ConfigureConsumer<CommitStockReservationConsumer>(context));
        cfg.ReceiveEndpoint("release-stock-reservation", e =>
            e.ConfigureConsumer<ReleaseStockReservationConsumer>(context));
    }, typeof(CommitStockReservationConsumer).Assembly);

builder.Services.AddSingleton(sp => new StockDbContext(builder.Configuration.GetConnectionString("stock")!, "stock"));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();

var host = builder.Build();
host.Run();
