using Microsoft.EntityFrameworkCore;
using Domain.Common.States;
using Application.Saga;
using MassTransit;
using Data;


var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();










builder.Logging.AddConsole();
LogContext.ConfigureCurrentLogContext();

builder.Services.AddMassTransit(cfg =>
{
    cfg.SetKebabCaseEndpointNameFormatter();

    cfg.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .InMemoryRepository();

    cfg.UsingRabbitMq((context, bus) =>
    {
        bus.Host(new Uri(builder.Configuration.GetConnectionString("rabbit")!));
        bus.ConfigureEndpoints(context);
    });
});


var host = builder.Build();
host.Run();

//// TODO: Move this to service defaults.
//builder.Services.AddMassTransit(config =>
//{
//    config.SetKebabCaseEndpointNameFormatter();

//    config.AddSagaStateMachine<OrderStateMachine, OrderState>()
//        .EntityFrameworkRepository(r =>
//        {
//            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
//            r.AddDbContext<DbContext, OrderSagaDbContext>((provider, options) =>
//            {
//                options.UseNpgsql(builder.Configuration.GetConnectionString("orders"));
//                options.EnableSensitiveDataLogging();
//            });
//        });

//    config.AddConsumers(typeof(OrderStateMachine).Assembly);

//    if (builder.Environment.IsDevelopment())
//    {
//        config.UsingRabbitMq((context, cfg) =>
//        {
//            cfg.Host(new Uri(builder.Configuration.GetConnectionString("rabbit")!));

//            cfg.UseDelayedMessageScheduler();
//            cfg.ConfigureEndpoints(context);
//        });
//    }
//    else
//    {
//        config.UsingAzureServiceBus((context, cfg) =>
//        {
//            cfg.Host(builder.Configuration.GetConnectionString("servicebus"));

//            cfg.UseServiceBusMessageScheduler();
//            cfg.ConfigureEndpoints(context);
//        });
//    }
//});
//builder.Services.AddDbContext<OrderSagaDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("orders")));

//builder.Services.AddHostedService<Worker>();

//var host = builder.Build();

//using (var scope = host.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
//    db.Database.Migrate();
//}

//host.Run();
