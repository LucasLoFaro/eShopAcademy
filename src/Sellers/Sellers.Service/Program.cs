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

        cfg.ReceiveEndpoint("seller-document-verification", e =>
            e.ConfigureConsumer<SellerDocumentVerificationConsumer>(context));

        cfg.ReceiveEndpoint("seller-tax-billing-verification", e =>
            e.ConfigureConsumer<SellerTaxBillingVerificationConsumer>(context));
    }, typeof(OrderSubmittedForSellerConsumer).Assembly);

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();

var host = builder.Build();
host.Run();
