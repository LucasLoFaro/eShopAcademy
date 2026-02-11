using Application.Saga;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestration.Data;
using Quartz;

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
        // RabbitMQ with Quartz scheduler
        cfg.AddQuartzConsumers();
        cfg.UsingRabbitMq((context, bus) =>
        {
            bus.Host(new Uri(rabbitConnectionString));
            bus.UseMessageScheduler(new Uri("queue:quartz"));
            bus.ConfigureEndpoints(context);
        });
    }
    else
    {
        // Azure Service Bus with built-in scheduler
        cfg.UsingAzureServiceBus((context, bus) =>
        {
            bus.Host(serviceBusConnectionString);
            bus.UseServiceBusMessageScheduler();
            bus.ConfigureEndpoints(context);
        });
    }
});

// Quartz scheduler for RabbitMQ (dev environment)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddQuartz();
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
}

var host = builder.Build();


using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.Migrate();
}

host.Run();

