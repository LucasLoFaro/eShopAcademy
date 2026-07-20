using MassTransit;
using MongoDB.Driver;
using Sellers.Application.Repositories;
using Sellers.Service.Consumers;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration =>
        {
            registration.AddConsumer<OrderSubmittedForSellerConsumer>().ExcludeFromConfigureEndpoints();
            registration.AddConsumer<SellerDocumentVerificationConsumer>().ExcludeFromConfigureEndpoints();
            registration.AddConsumer<SellerTaxBillingVerificationConsumer>().ExcludeFromConfigureEndpoints();
        });
        messaging.ReceiveEndpoint<OrderSubmittedForSellerConsumer>("seller-orders-submitted", ConfigureEndpoint);
        messaging.ReceiveEndpoint<SellerDocumentVerificationConsumer>("seller-document-verification", ConfigureEndpoint);
        messaging.ReceiveEndpoint<SellerTaxBillingVerificationConsumer>("seller-tax-billing-verification", ConfigureEndpoint);
    });

builder.Services.AddSellerStorage(builder.Configuration);

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

static void ConfigureEndpoint(IReceiveEndpointConfigurator endpoint)
{
    endpoint.UseMessageRetry(retry =>
    {
        retry.Handle<MongoException>();
        retry.Handle<TimeoutException>();
        retry.Ignore<ArgumentException>();
        retry.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
    });
#pragma warning disable CS0618
    endpoint.UseInMemoryOutbox();
#pragma warning restore CS0618
}

public partial class Program;
