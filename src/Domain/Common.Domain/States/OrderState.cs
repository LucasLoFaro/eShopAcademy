using MassTransit;


namespace Domain.Common.States;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid ReservationId { get; set; }
    public decimal TotalAmount { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // Needed for EF optimistic concurrency in Postgres
    public byte[]? RowVersion { get; set; }
}