using Microsoft.EntityFrameworkCore;
using Order.Orchestration;
using Core.Domain.States;
using Application.Saga;
using MassTransit;
using Data;


var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMassTransit(config =>
{
    config.SetKebabCaseEndpointNameFormatter();

    config.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, OrderDbContext>((provider, options) =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("orders"));
            });
        });

    config.AddConsumers(typeof(OrderStateMachine).Assembly);

    if (builder.Environment.IsDevelopment())
    {
        config.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(new Uri(builder.Configuration.GetConnectionString("rabbit")!));

            cfg.UseDelayedMessageScheduler();
        });
    }
    else
    {
        config.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration.GetConnectionString("servicebus"));

            cfg.UseServiceBusMessageScheduler();
        });
    }
});
builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("orders")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
