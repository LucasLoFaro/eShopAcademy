using Customers.Infrastructure.Data;
using Customers.Messaging.Consumers;
using MassTransit;
using MongoDB.Driver;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration =>
            registration.AddConsumer<CustomerAddressUpdatedEventConsumer>().ExcludeFromConfigureEndpoints());
        messaging.ReceiveEndpoint<CustomerAddressUpdatedEventConsumer>("customer-address-updated", endpoint =>
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

builder.Services.AddCustomerStorage(builder.Configuration);

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

public partial class Program;
