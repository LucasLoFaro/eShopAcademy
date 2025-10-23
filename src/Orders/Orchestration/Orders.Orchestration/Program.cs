using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Domain.Common.States;
using Orchestration.Data;
using Application.Saga;
using MassTransit;



var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Logging.AddConsole();
LogContext.ConfigureCurrentLogContext();

builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("orchestration"));
    options.EnableSensitiveDataLogging();
});

// TODO: Move this to service defaults.
builder.Services.AddMassTransit(cfg =>
{
    cfg.SetKebabCaseEndpointNameFormatter();

    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, OrderSagaDbContext>((provider, options) =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("orchestration"));
            });
        });
    if (builder.Environment.IsDevelopment())
    {
        cfg.UsingRabbitMq((context, bus) =>
        {
            bus.Host(new Uri(builder.Configuration.GetConnectionString("rabbit")!));
            bus.UseDelayedMessageScheduler();
            bus.ConfigureEndpoints(context);
        });
    }
    else
    {
        cfg.UsingAzureServiceBus((context, bus) =>
        {
            bus.Host(builder.Configuration.GetConnectionString("servicebus"));
            bus.UseServiceBusMessageScheduler();
            bus.ConfigureEndpoints(context);
        });
    }
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.EnsureCreated();
}

host.Run();