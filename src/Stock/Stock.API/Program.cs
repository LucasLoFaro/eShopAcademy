using Infrastructure.Services;
using Infrastructure.Data;
using ServiceDefaults;


namespace API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults()
               .WithSwagger()
               .WithMassTransit();

        builder.Services.AddControllers()
            .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

        builder.Services.AddSingleton(sp => new StockDbContext(builder.Configuration.GetConnectionString("stock"), "stock"));
        builder.Services.AddScoped<IStockRepository, StockRepository>();

        builder.Services.AddTransient<StockMessagingClient>();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
            await SeedTestData(app);

        app.MapControllers();
        app.UseDefaultEndpoints();
        app.Run();
    }

    private static async Task SeedTestData(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
            var messaging = scope.ServiceProvider.GetRequiredService<StockMessagingClient>();
            await StockSeedData.InitializeAsync(db, messaging);
        }
    }
}