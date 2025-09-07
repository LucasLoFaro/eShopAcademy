using Infrastructure.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Environment.ApplicationName = "order.api";

builder.AddServiceDefaults()
       .AddSwagger()
       .WithMassTransit();

builder.Services.AddSingleton<OrderMessagingClient>();

var app = builder.Build();
app.MapDefaultEndpoints();
app.Run();
