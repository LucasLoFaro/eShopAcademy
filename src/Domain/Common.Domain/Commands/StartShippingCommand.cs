namespace Domain.Common.Commands;

public class StartShippingCommand : BaseCommand
{
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public Guid StockReservationId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string ShippingAddress { get; init; } = string.Empty;
}
