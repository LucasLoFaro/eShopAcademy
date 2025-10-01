using Core.Application.Interfaces.Services;
using Core.Application.Interfaces.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Core.Application.Services;
using Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Infrastructure.Data;
using ServiceDefaults;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddControllers();

//Inject services
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseCosmos(
        builder.Configuration.GetConnectionString("cosmosdb")!,
        databaseName: "eShopAcademy",
        cosmosOptions =>
        {
            cosmosOptions.HttpClientFactory(() =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                return new HttpClient(handler);
            });
            cosmosOptions.ConnectionMode(ConnectionMode.Gateway);
        }));
builder.Services.AddTransient<IProductMessagingService, ProductMessagingService>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await SeedTestData(app);

app.MapControllers();
app.UseDefaultEndpoints();

app.Run();

static async Task SeedTestData(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.EnsureCreatedAsync();

        var messagingService = scope.ServiceProvider.GetRequiredService<IProductMessagingService>();
        await SeedData.InitializeAsync(db, messagingService);
    }
}