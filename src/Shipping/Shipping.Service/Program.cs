using MassTransit;
using Microsoft.Extensions.Configuration;
using ServiceDefaults;
using Shipping.Application.Data;
using Shipping.Service.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("shipping");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("The shipping MongoDB connection string is not configured.");
    }

    var databaseName = configuration["Shipping:Database"] ?? "shipping";

    return new ShippingDbContext(connectionString, databaseName);
});

builder.Services.AddScoped<IShippingInfoRepository, ShippingInfoRepository>();

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("schedule-shipping", e =>
            e.ConfigureConsumer<ScheduleShippingCommandConsumer>(context));
        cfg.ReceiveEndpoint("cancel-shipping", e =>
            e.ConfigureConsumer<CancelShippingCommandConsumer>(context));
        cfg.ReceiveEndpoint("order-delivered", e =>
            e.ConfigureConsumer<OrderDeliveredEventConsumer>(context));
    }, typeof(ScheduleShippingCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
