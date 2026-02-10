using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ServiceDefaults;
using Shipping.Application.Clients;
using Shipping.Application.Data;
using Shipping.Application.Options;
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
builder.Services.Configure<ShippingProviderOptions>(builder.Configuration.GetSection("Shipping:Provider"));
builder.Services.AddHttpClient<IShippingProviderClient, ShippingProviderClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ShippingProviderOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("The shipping provider base URL is not configured.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("schedule-shipping", e =>
            e.ConfigureConsumer<ScheduleShippingCommandConsumer>(context));
        cfg.ReceiveEndpoint("cancel-shipping", e =>
            e.ConfigureConsumer<CancelShippingCommandConsumer>(context));
        cfg.ReceiveEndpoint("order-delivered", e =>
            e.ConfigureConsumer<OrderDeliveredEventConsumer>(context));
        cfg.ReceiveEndpoint("confirm-shipping", e =>
            e.ConfigureConsumer<ConfirmShippingCommandConsumer>(context));
    }, typeof(ScheduleShippingCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
