using Domain.Common.States;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestration.Data;

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
            retry.Immediate(5);
        });
        endpointConfigurator.UseEntityFrameworkOutbox<OrderSagaDbContext>(context);
    }
}
