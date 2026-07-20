using Application.Saga;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchestration.Data;
using Quartz;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Logging.AddConsole();
LogContext.ConfigureCurrentLogContext();

var orchestrationConnectionString = builder.Configuration.GetConnectionString("orchestration")
    ?? throw new OptionsValidationException(
        "ConnectionStrings",
        typeof(string),
        ["ConnectionStrings:orchestration is required."]);
var enableSensitiveDataLogging = builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Persistence:EnableSensitiveDataLogging");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<OrderSagaOptions>()
    .Bind(builder.Configuration.GetSection(OrderSagaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseNpgsql(orchestrationConnectionString);
    if (enableSensitiveDataLogging)
    {
        options.EnableSensitiveDataLogging();
    }
});

builder.WithMassTransit(messaging =>
{
    messaging.UseScheduler();
    messaging.Registration(registration =>
    {
        registration.AddEntityFrameworkOutbox<OrderSagaDbContext>(outbox =>
        {
            outbox.UsePostgres();
            outbox.UseBusOutbox();
        });

        registration.AddSagaStateMachine<OrderStateMachine, OrderState, OrderStateMachineDefinition>()
            .EntityFrameworkRepository(repository =>
            {
                repository.ConcurrencyMode = ConcurrencyMode.Optimistic;
                repository.AddDbContext<DbContext, OrderSagaDbContext>((_, options) =>
                    options.UseNpgsql(orchestrationConnectionString));
            });
    });
});

// ServiceDefaults adds Quartz only for RabbitMQ scheduling. Applying this after
// transport registration replaces Quartz's RAM store with a durable, clustered
// PostgreSQL job store. Azure Service Bus continues to use its native scheduler.
builder.Services.AddQuartz(quartz =>
{
    quartz.SchedulerName = "order-saga-scheduler";
    quartz.SchedulerId = "AUTO";
    quartz.UsePersistentStore(store =>
    {
        store.PerformSchemaValidation = true;
        store.UseProperties = true;
        store.UsePostgres(orchestrationConnectionString);
        store.UseSystemTextJsonSerializer();
        store.UseClustering(cluster =>
        {
            cluster.CheckinInterval = TimeSpan.FromSeconds(10);
            cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
        });
    });
});

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.Migrate();
}

host.Run();
