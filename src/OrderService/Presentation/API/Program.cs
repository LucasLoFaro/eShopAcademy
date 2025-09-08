using Infrastructure.Services;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger()
       .WithMassTransit();

builder.Services.AddControllers();

builder.Services.AddScoped<OrderMessagingClient>();

var app = builder.Build();
app.MapControllers();
app.UseDefaultEndpoints();
app.Run();

public partial class Program { }