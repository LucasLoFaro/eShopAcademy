using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Common.States;
using MassTransit;


namespace Orchestration.Data;

public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        entity.ToTable("order_saga_state");

        entity.HasKey(x => x.CorrelationId);

        entity.Property(x => x.CurrentState)
              .HasMaxLength(64)
              .IsRequired();

        entity.Property(x => x.OrderId);
        entity.Property(x => x.CustomerEmail)
              .HasMaxLength(256)
              .IsRequired();
        entity.Property(x => x.CustomerId);
        entity.Property(x => x.BasketClientId);
        entity.Property(x => x.PaymentId);
        entity.Property(x => x.ReservationId);
        entity.Property(x => x.TotalAmount);
        entity.Property(x => x.ProviderTransactionId)
              .HasMaxLength(256)
              .IsRequired();
        entity.Property(x => x.CustomerName)
              .HasMaxLength(256)
              .IsRequired();
        entity.Property(x => x.DestinationAddress)
              .HasMaxLength(512)
              .IsRequired();

        entity.Property(x => x.PaymentTimeoutTokenId);

        // Concurrency token in Postgres
        entity.Property(x => x.RowVersion)
              .IsRowVersion()
              .IsConcurrencyToken();

        entity.HasIndex(x => x.OrderId);
    }
}