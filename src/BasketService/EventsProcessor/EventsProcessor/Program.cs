using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Azure.Messaging.ServiceBus;
using EventsProcessor.Consumers;
using Data.Interfaces;
using Azure.Identity;
using MassTransit;
using Settings;
using Data;


var builder = Host.CreateApplicationBuilder(args);

var credential = new DefaultAzureCredential();

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(credential); })
    .Select("common:*", LabelFilter.Null)
    .Select("basket:*", LabelFilter.Null)
    );

var sbSettings = builder.Configuration.GetSection("common:ServiceBusSettings").Get<ServiceBusSettings>();
var sbClient = new ServiceBusClient($"sb://{sbSettings!.Host}/", credential);

builder.Services.AddMassTransit(x => {
    x.AddConsumer<ProductsEventConsumer>();
    x.AddConsumer<StockEventConsumer>();
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host((ServiceBusHostSettings) sbClient);
        cfg.ReceiveEndpoint("products-updated", e =>
        {
            e.ConfigureConsumer<ProductsEventConsumer>(context);
        });
        cfg.ReceiveEndpoint("stock-updated", e =>
        {
            e.ConfigureConsumer<StockEventConsumer>(context);
        });
    });
});
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("basket:Database"));
builder.Services.AddSingleton<DatabaseClient>();
builder.Services.AddTransient<IProductCache, ProductCache>();
builder.Services.AddAutoMapper(typeof(Program));

var host = builder.Build();
host.Run();
