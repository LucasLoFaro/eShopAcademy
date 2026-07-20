using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Customers.Infrastructure.Data;

public sealed class CustomerStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "customers";
}

public static class CustomerStorage
{
    public static IServiceCollection AddCustomerStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CustomerStorageOptions>()
            .Configure(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("customers") ?? string.Empty;
                options.DatabaseName = configuration["Customers:Database"] ?? "customers";
            })
            .Validate(options => options.ConnectionString.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
                                 options.ConnectionString.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase),
                "ConnectionStrings:customers must be a MongoDB connection string.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                "Customers:Database must not be empty.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CustomerStorageOptions>>().Value;
            return new CustomerDbContext(options.ConnectionString, options.DatabaseName);
        });
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddHealthChecks().AddCheck<CustomerMongoHealthCheck>(
            "customers-mongodb",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["ready"],
            timeout: TimeSpan.FromSeconds(3));
        return services;
    }
}

public sealed class CustomerMongoHealthCheck : IHealthCheck
{
    private readonly CustomerDbContext _database;

    public CustomerMongoHealthCheck(CustomerDbContext database) => _database = database;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.PingAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Customer storage is unavailable.");
        }
    }
}
