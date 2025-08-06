using Core.Domain.Commands;
using MassTransit;

namespace EventsProcessor.StateMachines;

/// <summary>
/// Defines the saga state machine for order submission.  This simple
/// implementation handles the initial SubmitOrder command and transitions
/// the saga into the Submitted state.  Additional states and events can
/// be added later to coordinate with stock and payment services.
/// </summary>
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = null!;

    public Event<SubmitOrder> SubmitOrderEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        // Map the CurrentState property of OrderState to the saga state
        // machine's internal state representation.
        InstanceState(x => x.CurrentState);

        // Define the SubmitOrder event and how to correlate it to a saga
        // instance.  MassTransit will use the OrderId property on the
        // incoming message to find or create the appropriate OrderState.
        Event(() => SubmitOrderEvent, x =>
        {
            x.CorrelateById(context => context.Message.OrderId);
            x.InsertOnInitial = true;
            x.SetSagaFactory(context => new OrderState
            {
                CorrelationId = context.Message.OrderId,
                CustomerId = context.Message.CustomerId,
                Items = context.Message.Items
            });
        });

        // When the saga starts (no prior state) and a SubmitOrder event
        // arrives, initialize the saga's internal data and transition it
        // into the Submitted state.
        Initially(
            When(SubmitOrderEvent)
                .Then(context =>
                {
                    context.Instance.CustomerId = context.Data.CustomerId;
                    context.Instance.Items = context.Data.Items;
                })
                .TransitionTo(Submitted)
        );
    }
}