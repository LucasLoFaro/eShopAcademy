using Core.Domain.Entities;
using MassTransit;

namespace EventsProcessor.StateMachines;

/// <summary>
/// Represents the state data for an order saga.  MassTransit stores an
/// instance of this class for each running saga.  The CorrelationId
/// property is used by MassTransit to route incoming events to the
/// correct saga instance.
/// </summary>
public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public Guid CustomerId { get; set; }

    public List<Item> Items { get; set; } = new();

    /// <summary>
    /// The current state name as persisted by MassTransit.  The state
    /// machine updates this field automatically via InstanceState().
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;
}