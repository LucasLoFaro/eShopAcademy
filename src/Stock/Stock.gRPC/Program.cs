using gRPC.Services;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ServiceDefaults;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithMassTransit();

builder.Services.AddGrpc();
builder.Services.AddSingleton(sp => new StockDbContext(builder.Configuration.GetConnectionString("stock"), "stock"));
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStockReservationRepository, StockReservationRepository>();
builder.Services.AddTransient<StockMessagingClient>();


builder.WebHost.ConfigureKestrel(o =>
{
    o.ConfigureEndpointDefaults(lo => lo.Protocols = HttpProtocols.Http2);
});

var app = builder.Build();

app.MapGrpcService<StockService>();
app.MapGet("/", () => "Mock endpoint");

app.Run();
