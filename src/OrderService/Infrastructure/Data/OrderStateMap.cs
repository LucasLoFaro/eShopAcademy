using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Core.Domain.States;
using MassTransit;


namespace Data;

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState);
        entity.Property(x => x.OrderId);
        entity.Property(x => x.PaymentId);
        entity.Property(x => x.ReservationId);
        entity.Property(x => x.CreatedAt);
        entity.Property(x => x.ExpirationToken);
    }
}