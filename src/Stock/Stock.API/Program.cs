using Infrastructure.Services;
using Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceDefaults;
using OpenTelemetry.Metrics;


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
        builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter(StockMetrics.MeterName));
        builder.Services.AddProblemDetails();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                .AllowAnyHeader()
                .AllowAnyMethod()));

        var connectionString = builder.Configuration.GetConnectionString("stock");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("The stock MongoDB connection string is not configured.");

        var stockDatabase = new StockDbContext(connectionString, "stock");
        builder.Services.AddSingleton(stockDatabase);
        builder.Services.AddHealthChecks().AddAsyncCheck(
            "stock-database",
            async cancellationToken =>
            {
                await stockDatabase.PingAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            },
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));
        builder.Services.AddScoped<IStockRepository, StockRepository>();
        builder.Services.AddScoped<IStockOperationStore, StockOperationStore>();

        builder.Services.AddTransient<StockMessagingClient>();

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
            await SeedTestData(app);

        app.UseExceptionHandler();
        app.UseCors();
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
