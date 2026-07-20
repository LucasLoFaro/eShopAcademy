using Domain.Common.States;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Orchestration.Data;

public class OrderSagaDbContext : SagaDbContext
{
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options)
        : base(options) { }

    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new OrderStateMap() };

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddTransactionalOutboxEntities();
    }

    public DbSet<OrderState> OrderStates => Set<OrderState>();
}
