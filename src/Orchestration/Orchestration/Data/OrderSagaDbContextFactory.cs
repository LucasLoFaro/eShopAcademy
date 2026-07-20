using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Orchestration.Data;

public sealed class OrderSagaDbContextFactory : IDesignTimeDbContextFactory<OrderSagaDbContext>
{
    public OrderSagaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationManager();
        configuration.AddEnvironmentVariables();
        configuration.AddCommandLine(args);

        var options = new DbContextOptionsBuilder<OrderSagaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("orchestration"))
            .Options;

        return new OrderSagaDbContext(options);
    }
}
