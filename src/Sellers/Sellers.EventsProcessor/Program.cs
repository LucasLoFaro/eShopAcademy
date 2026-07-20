using MassTransit;
using MongoDB.Driver;
using Sellers.Application.Repositories;
using Sellers.Application.Services;
using Sellers.EventsProcessor.Consumers;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration =>
            registration.AddConsumer<OrderSellerSaleRegistrationRequestedConsumer>().ExcludeFromConfigureEndpoints());
        messaging.ReceiveEndpoint<OrderSellerSaleRegistrationRequestedConsumer>("seller-sale-registration-requested", endpoint =>
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
        });
    });

builder.Services.AddSellerStorage(builder.Configuration);
builder.Services.AddScoped<ISellerService, SellerService>();

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

public partial class Program;
