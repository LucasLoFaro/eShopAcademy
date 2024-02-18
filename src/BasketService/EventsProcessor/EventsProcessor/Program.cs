using MassTransit;
using Settings;
using AutoMapper;
using Data.Interfaces;
using Data;
using EventsProcessor.Consumers;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("basket:*", LabelFilter.Null)
    );

var settings = builder.Configuration.GetSection("common:RabbitMQSettings").Get<RabbitMQSettings>();
builder.Services.AddMassTransit(x => {
    x.AddConsumer<ProductsEventConsumer>();
    x.AddConsumer<StockEventConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(settings.Host, "/", h =>
        {
            h.Username(settings.Username);
            h.Password(settings.Password);
        });
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
