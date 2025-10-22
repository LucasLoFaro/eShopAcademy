using MassTransit;


namespace Domain.Common.States;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;

    // Needed for EF optimistic concurrency in Postgres
    public byte[]? RowVersion { get; set; }
}