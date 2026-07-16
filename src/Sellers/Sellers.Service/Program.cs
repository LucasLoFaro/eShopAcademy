using Sellers.Application.Repositories;
using Sellers.Service.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.ReceiveEndpoint<OrderSubmittedForSellerConsumer>("seller-orders-submitted");
        messaging.ReceiveEndpoint<SellerDocumentVerificationConsumer>("seller-document-verification");
        messaging.ReceiveEndpoint<SellerTaxBillingVerificationConsumer>("seller-tax-billing-verification");
    }, typeof(OrderSubmittedForSellerConsumer).Assembly);

builder.Services.AddSingleton<ISellerRepository, SellerRepository>();

var host = builder.Build();
host.Run();
