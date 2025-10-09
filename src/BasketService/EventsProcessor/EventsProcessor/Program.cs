using ServiceDefaults;
using Data.Interfaces;
using MassTransit;
using Consumers;
using Data;


var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    { 
        cfg.ReceiveEndpoint("products-updated", e => e.ConfigureConsumer<ProductsEventConsumer>(context));
        cfg.ReceiveEndpoint("stock-updated", e => e.ConfigureConsumer<StockEventConsumer>(context));
    },
    typeof(ProductsEventConsumer).Assembly);

//Inject services
builder.Services.AddSingleton<IDatabaseClient>(sp => new DatabaseClient(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddTransient<IBasketCache, BasketCache>();
builder.Services.AddTransient<IProductCache, ProductCache>();

var host = builder.Build();
host.Run();
