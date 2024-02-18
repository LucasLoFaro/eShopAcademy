using Azure.Identity;
using gRPC.Services;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(
        new Uri($"https://{Environment.GetEnvironmentVariable("APPCONFIGURATION")}.azconfig.io"),
        new DefaultAzureCredential())
    .ConfigureKeyVault(kv => { kv.SetCredential(new DefaultAzureCredential()); })
    .Select("common:*", LabelFilter.Null)
    .Select("stock:*", LabelFilter.Null)
    );

// Add services to the container.
builder.Services.AddGrpc(opt => { opt.EnableDetailedErrors = true; });
builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseMongoDB(builder.Configuration["stock:ConnectionStrings:MongoDB"], "eShopAcademy")
);
builder.Services.AddScoped<IStockRepository, StockRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
//!! Desactivar IIS Express porque necesita correr en Http2
app.MapGrpcService<StockService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
