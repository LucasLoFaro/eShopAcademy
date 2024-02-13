using EventsProcessor;
using MassTransit;
using Settings;
using AutoMapper;
using Data.Interfaces;
using Data;

var builder = Host.CreateApplicationBuilder(args);

var settings = builder.Configuration.GetSection("RabbitMQSettings").Get<RabbitMQSettings>();
builder.Services.AddMassTransit(x => {
    x.AddConsumer<ProductsEventConsumer>();
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
    });
});
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<DatabaseClient>();
builder.Services.AddTransient<IProductCache, ProductCache>();
//builder.Services.AddMassTransitHostedService();
builder.Services.AddAutoMapper(typeof(Program));

var host = builder.Build();
host.Run();
