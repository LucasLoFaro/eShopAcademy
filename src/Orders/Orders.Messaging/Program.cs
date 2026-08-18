using Application.Observability;
using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Orders.Messaging.Consumers;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

var ordersConnectionString = builder.Configuration.GetConnectionString("orders");
if (string.IsNullOrWhiteSpace(ordersConnectionString))
    throw new InvalidOperationException("ConnectionStrings:orders is required.");

builder.AddServiceDefaults()
    .WithMassTransit(messaging =>
    {
        messaging.Registration(registration =>
        {
            registration.AddEntityFrameworkOutbox<OrderDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });
        });
        messaging.ReceiveEndpoint<CancelOrderCommandConsumer>(
            "cancel-order-command",
            endpoint => endpoint.ConcurrentMessageLimit = 1);
        messaging.ReceiveEndpoint<UpdateOrderStatusCommandConsumer>(
            "update-order-status-command",
            endpoint => endpoint.ConcurrentMessageLimit = 1);
    }, typeof(CancelOrderCommandConsumer).Assembly);

builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(OrdersTelemetry.MeterName));
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(ordersConnectionString));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddHealthChecks().AddCheck<OrderDatabaseHealthCheck>(
    "orders-db",
    tags: ["ready"],
    timeout: TimeSpan.FromSeconds(3));

var app = builder.Build();
app.UseDefaultEndpoints();
app.Run();

public partial class Program { }
