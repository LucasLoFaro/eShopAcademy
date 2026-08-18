using Core.Application.Interfaces.Services;
using Core.Application.Interfaces.Data;
using Infrastructure.Data.Repositories;
using Core.Application.Services;
using Infrastructure.Services;
using Infrastructure.Data;
using ServiceDefaults;
using Data;


var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults()
       .WithMassTransit()
       .WithSwagger();

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

//Inject services
builder.Services.AddProductStorage(builder.Configuration);
builder.Services.AddTransient<IProductMessagingService, ProductMessagingService>();
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.AddProductContentModeration(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseExceptionHandler();

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
        var messagingService = scope.ServiceProvider.GetRequiredService<IProductMessagingService>();
        await ProductSeedData.InitializeAsync(db, messagingService);
    }
}

public partial class Program;
