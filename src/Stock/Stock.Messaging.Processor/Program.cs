using Infrastructure.Data;
using Infrastructure.Services;
using ServiceDefaults;
using Stock.Messaging.Processor.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<CommitStockReservationConsumer>("commit-stock-reservation");
        messaging.ReceiveEndpoint<ReleaseStockReservationConsumer>("release-stock-reservation");
        messaging.ReceiveEndpoint<ProductPublishedConsumer>("product-published-stock");
    }, typeof(CommitStockReservationConsumer).Assembly);

builder.Services.AddSingleton(sp => new StockDbContext(builder.Configuration.GetConnectionString("stock")!, "stock"));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();

var host = builder.Build();
host.Run();
