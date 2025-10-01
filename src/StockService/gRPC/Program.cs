using Infrastructure.Data;
using gRPC.Services;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpc(opt => { opt.EnableDetailedErrors = true; });
builder.Services.AddSingleton(sp => new StockDbContext(builder.Configuration.GetConnectionString("stock"), "stock"));
builder.Services.AddScoped<IStockRepository, StockRepository>();


var app = builder.Build();

app.MapGrpcService<StockService>();
app.MapGet("/", () => "Mock endpoint");

app.Run();
