using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orchestration.Data;

public sealed class OrderSagaDbContextFactory : IDesignTimeDbContextFactory<OrderSagaDbContext>
{
    public OrderSagaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderSagaDbContext>()
            .UseNpgsql("Host=localhost;Database=order_saga_design;Username=postgres;Password=postgres")
            .Options;

        return new OrderSagaDbContext(options);
    }
}
