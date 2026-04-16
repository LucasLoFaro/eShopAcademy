using MassTransit;
using Sellers.Application.Repositories;
using Sellers.Service.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("seller-orders-submitted", e =>
            e.ConfigureConsumer<OrderSubmittedForSellerConsumer>(context));
    }, typeof(OrderSubmittedForSellerConsumer).Assembly);

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();

var host = builder.Build();
host.Run();
