using Application.Managers;
using Data;
using gRPC.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc(opt => { opt.EnableDetailedErrors = true;});
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseMongoDB("mongodb://admin:admin@localhost:27017/"/*Configuration.GetConnectionString("DefaultConnection"*/, "eShopAcademy")
);
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockManager, StockManager>();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//!! Desactivar IIS Express porque necesita correr en Http2
app.MapGrpcService<StockService>();
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
