using Core.Application.Interfaces.Data;
using Infrastructure.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Data;

public sealed class ProductStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "products";
}

public static class ProductStorage
{
    public static IServiceCollection AddProductStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProductStorageOptions>()
            .Configure(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("products") ?? string.Empty;
                options.DatabaseName = configuration["Products:Database"] ?? "products";
            })
            .Validate(options => options.ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
                                 options.ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase),
                "ConnectionStrings:products must be a MongoDB connection string.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "Products:Database must not be empty.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProductStorageOptions>>().Value;
            return new ProductDbContext(options.ConnectionString, options.DatabaseName);
        });
        services.AddTransient<IProductsRepository, ProductsRepository>();
        services.AddHealthChecks().AddCheck<ProductMongoHealthCheck>(
            "products-mongodb",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));
        return services;
    }
}

public sealed class ProductMongoHealthCheck : IHealthCheck
{
    private readonly ProductDbContext _database;

    public ProductMongoHealthCheck(ProductDbContext database) => _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Product storage is unavailable.");
        }
    }
}
