using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class OrderSagaDbContext : SagaDbContext
{
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options)
        : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new OrderStateMap() };
}