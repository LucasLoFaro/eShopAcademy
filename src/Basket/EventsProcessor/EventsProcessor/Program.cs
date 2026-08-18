using Basket.EventsProcessor.Consumers;
using Data;
using MassTransit;
using ServiceDefaults;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration =>
        {
            registration.AddConsumer<ProductsEventConsumer>().ExcludeFromConfigureEndpoints();
            registration.AddConsumer<StockEventConsumer>().ExcludeFromConfigureEndpoints();
            registration.AddConsumer<EmptyBasketCommandConsumer>().ExcludeFromConfigureEndpoints();
            registration.AddConsumer<ReinstateBasketCommandConsumer>().ExcludeFromConfigureEndpoints();
        });
        messaging.ReceiveEndpoint<ProductsEventConsumer>("products-updated", ConfigureEndpoint);
        messaging.ReceiveEndpoint<StockEventConsumer>("stock-updated", ConfigureEndpoint);
        messaging.ReceiveEndpoint<EmptyBasketCommandConsumer>("empty-basket", ConfigureEndpoint);
        messaging.ReceiveEndpoint<ReinstateBasketCommandConsumer>("reinstate-basket", ConfigureEndpoint);
    });

builder.Services.AddBasketStorage(builder.Configuration, includeProductCache: true);

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

static void ConfigureEndpoint(IReceiveEndpointConfigurator endpoint)
{
    endpoint.UseMessageRetry(retry =>
    {
        retry.Handle<RedisException>();
        retry.Handle<TimeoutException>();
        retry.Ignore<ArgumentException>();
        retry.Intervals(TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
    });
}

public partial class Program;
