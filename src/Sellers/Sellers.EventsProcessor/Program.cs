using Sellers.Application.Repositories;
using Sellers.Application.Services;
using Sellers.EventsProcessor.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<OrderSellerSaleRegistrationRequestedConsumer>("seller-sale-registration-requested");
    }, typeof(OrderSellerSaleRegistrationRequestedConsumer).Assembly);

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<ISellerService, SellerService>();

var host = builder.Build();
host.Run();
