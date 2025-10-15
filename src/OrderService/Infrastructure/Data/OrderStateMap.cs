using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Order.States;
using MassTransit;


namespace Data;

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.HasKey(x => x.CorrelationId);
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.OrderId);
        entity.Property(x => x.CustomerEmail).HasMaxLength(256);
    }
}