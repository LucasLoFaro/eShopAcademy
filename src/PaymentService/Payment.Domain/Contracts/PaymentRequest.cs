namespace Domain.Contracts;

public class PaymentRequest
{
    public string OrderId { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string NotificationUrl { get; init; } = default!;
}
