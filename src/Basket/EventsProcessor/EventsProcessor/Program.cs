using Basket.EventsProcessor.Consumers;
using Data;
using Data.Interfaces;
using ServiceDefaults;


var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<ProductsEventConsumer>("products-updated");
        messaging.ReceiveEndpoint<StockEventConsumer>("stock-updated");
        messaging.ReceiveEndpoint<EmptyBasketCommandConsumer>("empty-basket");
        messaging.ReceiveEndpoint<ReinstateBasketCommandConsumer>("reinstate-basket");
    },
    typeof(ProductsEventConsumer).Assembly);

//Inject services
builder.Services.AddSingleton<IDatabaseClient>(sp => new DatabaseClient(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddTransient<IBasketCache, BasketCache>();
builder.Services.AddTransient<IProductCache, ProductCache>();

var host = builder.Build();
host.Run();
