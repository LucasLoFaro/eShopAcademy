using Application.Saga;
using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestration.Data;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Logging.AddConsole();
LogContext.ConfigureCurrentLogContext();

var orchestrationConnectionString = builder.Configuration.GetConnectionString("orchestration");

builder.Services.AddDbContext<OrderSagaDbContext>(options =>
{
    options.UseNpgsql(orchestrationConnectionString);
    options.EnableSensitiveDataLogging();
});

builder.WithMassTransit(messaging =>
{
    messaging.UseScheduler();
    messaging.Registration(registration =>
    {
        registration.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .EntityFrameworkRepository(repository =>
            {
                repository.ConcurrencyMode = ConcurrencyMode.Optimistic;
                repository.AddDbContext<DbContext, OrderSagaDbContext>((_, options) =>
                    options.UseNpgsql(orchestrationConnectionString));
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
