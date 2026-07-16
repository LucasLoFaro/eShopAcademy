using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Orders.Messaging.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
.WithMassTransit(messaging =>
{
    messaging.ReceiveEndpoint<CancelOrderCommandConsumer>("cancel-order-command");
    messaging.ReceiveEndpoint<UpdateOrderStatusCommandConsumer>(
        "update-order-status-command",
        endpoint => endpoint.ConcurrentMessageLimit = 1);
}, typeof(CancelOrderCommandConsumer).Assembly);

var ordersConnectionString = builder.Configuration.GetConnectionString("orders");

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(ordersConnectionString);
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var host = builder.Build();
host.Run();
