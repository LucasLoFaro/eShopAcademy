using Application.Saga;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestration.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Logging.AddConsole();
LogContext.ConfigureCurrentLogContext();

var orchestrationConnectionString = builder.Configuration.GetConnectionString("orchestration");
var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbit");
var serviceBusConnectionString = builder.Configuration.GetConnectionString("servicebus");

builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseNpgsql(orchestrationConnectionString);
    options.EnableSensitiveDataLogging();
});

builder.Services.AddMassTransit(cfg =>
{
    cfg.SetKebabCaseEndpointNameFormatter();

    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, OrderSagaDbContext>((provider, options) =>
            {
                options.UseNpgsql(orchestrationConnectionString);
            });
        });

    if (builder.Environment.IsDevelopment())
    {
        cfg.UsingRabbitMq((context, bus) =>
        {
            bus.Host(new Uri(rabbitConnectionString));
            bus.UseDelayedMessageScheduler();
            bus.ConfigureEndpoints(context);
        });
    }
    else
    {
        cfg.UsingAzureServiceBus((context, bus) =>
        {
            bus.Host(serviceBusConnectionString);
            bus.UseServiceBusMessageScheduler();
            bus.ConfigureEndpoints(context);
        });
    }
});

var host = builder.Build();


using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.Migrate();
}

host.Run();

