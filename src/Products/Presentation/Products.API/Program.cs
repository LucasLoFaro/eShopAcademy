using Core.Application.Interfaces.Services;
using Core.Application.Interfaces.Data;
using Infrastructure.Data.Repositories;
using Core.Application.Services;
using Infrastructure.Services;
using Infrastructure.Data;
using Domain.Products.Entities;
using MongoDB.Driver;
using ServiceDefaults;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddControllers()
    .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

//Inject services
builder.Services.AddSingleton(sp => new ProductDbContext(builder.Configuration.GetConnectionString("products"), "products"));
builder.Services.AddTransient<IProductMessagingService, ProductMessagingService>();
builder.Services.AddTransient<IProductsRepository, ProductsRepository>();
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.AddSingleton<IContentModerationService, ContentModerationService>();

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

        if (await db.Products.Find(Builders<Product>.Filter.Empty).AnyAsync())
            return;

        var messagingService = scope.ServiceProvider.GetRequiredService<IProductMessagingService>();
        await ProductSeedData.InitializeAsync(db, messagingService);
    }
}