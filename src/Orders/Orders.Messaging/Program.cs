using Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orders.Messaging.Consumers;
using ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults()
.WithMassTransit((context, cfg) =>
{
    cfg.ReceiveEndpoint("cancel-order-command", e =>
        e.ConfigureConsumer<CancelOrderCommandConsumer>(context));
    cfg.ReceiveEndpoint("update-order-status-command", e =>
        e.ConfigureConsumer<UpdateOrderStatusCommandConsumer>(context));
}, typeof(CancelOrderCommandConsumer).Assembly);

var ordersConnectionString = builder.Configuration.GetConnectionString("orders");

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(ordersConnectionString);
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var host = builder.Build();
host.Run();
