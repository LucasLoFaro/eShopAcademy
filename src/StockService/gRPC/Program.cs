using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using gRPC.Services;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc(opt => { opt.EnableDetailedErrors = true; });
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseMongoDB(builder.Configuration.GetConnectionString("mongodb")!, "stock")
);
builder.Services.AddScoped<IStockRepository, StockRepository>();


var app = builder.Build();

app.MapGrpcService<StockService>();
app.MapGet("/", () => "Mock endpoint");

app.Run();
