using MassTransit;


namespace States;

public class StockReservationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public Guid OrderId { get; set; }
    public bool IsCommitted { get; set; }
    public bool IsCancelled { get; set; }

    public Guid? ExpirationToken { get; set; }
    public DateTime CreatedAt { get; set; }
}