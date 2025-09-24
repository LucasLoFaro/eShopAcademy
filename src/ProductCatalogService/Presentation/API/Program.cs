using Core.Application.Interfaces.Data;
using Core.Application.Interfaces.Services;
using Core.Application.Services;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults;


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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();
app.UseDefaultEndpoints();

app.Run();