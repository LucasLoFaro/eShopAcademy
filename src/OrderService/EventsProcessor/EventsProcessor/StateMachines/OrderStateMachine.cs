using Core.Domain.Events;
using MassTransit;

namespace EventsProcessor.StateMachines;


public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = null!;

    public Event<OrderSubmittedEvent> SubmitOrderEvent { get; private set; } = null!;

    public OrderStateMachine()
    {
        // TODO.
    }
}