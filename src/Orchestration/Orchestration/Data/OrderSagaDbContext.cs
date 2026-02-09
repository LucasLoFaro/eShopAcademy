using Domain.Common.States;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Orchestration.Data;

public class OrderSagaDbContext : SagaDbContext
{
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options)
        : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new OrderStateMap() };

    public DbSet<OrderState> OrderStates => Set<OrderState>();
}