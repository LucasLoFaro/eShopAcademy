using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Sellers.Application.Repositories;

public sealed class SellerStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "sellers";
    public string CollectionName { get; set; } = "sellers";
}

public static class SellerStorage
{
    public static IServiceCollection AddSellerStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SellerStorageOptions>()
            .Configure(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("sellers") ?? string.Empty;
                options.DatabaseName = configuration["Sellers:Database"] ?? "sellers";
                options.CollectionName = configuration["Sellers:SellersCollection"] ?? "sellers";
            })
            .Validate(options => options.ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
                                 options.ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase),
                "ConnectionStrings:sellers must be a MongoDB connection string.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName) &&
                                 !string.IsNullOrWhiteSpace(options.CollectionName),
                "Seller database and collection names must not be empty.")
            .ValidateOnStart();

        services.AddSingleton<ISellerRepository, SellerRepository>();
        services.AddHealthChecks().AddCheck<SellerMongoHealthCheck>(
            "sellers-mongodb",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));
        return services;
    }
}

public sealed class SellerMongoHealthCheck : IHealthCheck
{
    private readonly IOptions<SellerStorageOptions> _options;

    public SellerMongoHealthCheck(IOptions<SellerStorageOptions> options) => _options = options;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = _options.Value;
            var database = new MongoClient(options.ConnectionString).GetDatabase(options.DatabaseName);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Seller storage is unavailable.");
        }
    }
}
