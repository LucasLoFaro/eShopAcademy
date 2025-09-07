using EventsProcessor.StateMachines;
using ServiceDefaults;
using MassTransit;


var builder = Host.CreateApplicationBuilder(args);
builder.Environment.ApplicationName = "orders.events-processor";
builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) =>
    {
        cfg.ReceiveEndpoint("submit-order", e => e.ConfigureSaga<OrderState>(context));
    },
    typeof(Program).Assembly);

var host = builder.Build();
await host.RunAsync();