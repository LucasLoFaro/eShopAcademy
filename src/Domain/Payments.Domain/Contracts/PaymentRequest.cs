namespace Domain.Payments.Contracts;

public class PaymentRequest
{
    public string ExternalId { get; init; } = default!;
    public double Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string NotificationUrl { get; init; } = default!;
}
