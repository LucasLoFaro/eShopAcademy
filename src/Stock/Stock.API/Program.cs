using Infrastructure.Services;
using Infrastructure.Data;
using ServiceDefaults;
using Microsoft.Extensions.Options;


namespace API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddWebServiceDefaults()
               .AddRequiredConnectionString("stock")
               .WithSwagger()
               .WithMassTransit();

        builder.Services.AddControllers()
            .AddJsonOptions(opt => { opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()); });

        builder.Services.AddSingleton(sp => new StockDbContext(
            sp.GetRequiredService<IOptionsMonitor<RequiredConnectionString>>().Get("stock").Value,
            "stock"));
        builder.Services.AddScoped<IStockRepository, StockRepository>();

        builder.Services.AddHealthChecks().AddCriticalDependency(
            "stock-mongodb",
            async (sp, cancellationToken) =>
                await sp.GetRequiredService<StockDbContext>().PingAsync(cancellationToken));

        builder.Services.AddTransient<StockMessagingClient>();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
            await SeedTestData(app);

        app.UseWebServiceDefaults();
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
