using EventsProcessor.StateMachines;
using ServiceDefaults;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults()
    .WithMassTransit((context, cfg) => { 
        cfg.ReceiveEndpoint("submit-order", e => e.ConfigureSaga<OrderState>(context));}, 
        typeof(OrderState).Assembly);

var host = builder.Build();
await host.RunAsync();