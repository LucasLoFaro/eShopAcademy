using MassTransit;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using Sellers.EventsProcessor.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("seller-sale-registration-requested", e =>
            e.ConfigureConsumer<OrderSellerSaleRegistrationRequestedConsumer>(context));
    }, typeof(OrderSellerSaleRegistrationRequestedConsumer).Assembly);

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<ISellerService, SellerService>();

var host = builder.Build();
host.Run();
