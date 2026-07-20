using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestration.Data;
using Orchestration.Observability;

namespace Application.Saga;

public sealed class OrderStateMachineDefinition : SagaDefinition<OrderState>
{
    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<OrderState> sagaConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Handle<DbUpdateConcurrencyException>();
            retry.Handle<Npgsql.NpgsqlException>(exception => exception.IsTransient);
            retry.Immediate(5);
        });
        endpointConfigurator.UseEntityFrameworkOutbox<OrderSagaDbContext>(context);
    }
}
