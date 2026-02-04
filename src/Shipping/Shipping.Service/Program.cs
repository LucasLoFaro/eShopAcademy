using MassTransit;
using ServiceDefaults;
using Shipping.Service.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("schedule-shipping", e =>
            e.ConfigureConsumer<ScheduleShippingCommandConsumer>(context));
        cfg.ReceiveEndpoint("cancel-shipping", e =>
            e.ConfigureConsumer<CancelShippingCommandConsumer>(context));
    }, typeof(ScheduleShippingCommandConsumer).Assembly);

var host = builder.Build();
host.Run();
