using Application.Saga;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Orchestration.Data;
using Orchestration.Health;
using Orchestration.Observability;
using Quartz;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
LogContext.ConfigureCurrentLogContext();

var orchestrationConnectionString = builder.Configuration.GetConnectionString("orchestration")
    ?? throw new OptionsValidationException(
        "ConnectionStrings",
        typeof(string),
        ["ConnectionStrings:orchestration is required."]);
var enableSensitiveDataLogging = builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Persistence:EnableSensitiveDataLogging");

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(OrderSagaTelemetry.MeterName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<OrderSagaOptions>()
    .Bind(builder.Configuration.GetSection(OrderSagaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseNpgsql(orchestrationConnectionString, npgsql => npgsql.CommandTimeout(5));
    if (enableSensitiveDataLogging)
        options.EnableSensitiveDataLogging();
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
                    options.UseNpgsql(orchestrationConnectionString, npgsql => npgsql.CommandTimeout(5)));
            });
    });
});

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

builder.Services.AddHealthChecks().AddCheck<OrderSagaDatabaseHealthCheck>(
    "saga-postgres",
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    database.Database.Migrate();
}

app.UseDefaultEndpoints();
app.Run();

public partial class Program { }
