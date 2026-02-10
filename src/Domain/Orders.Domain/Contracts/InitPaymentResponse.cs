namespace Domain.Orders.Contracts;

public record InitPaymentResponse
{
    public Guid Id { get; init; }
    public double Amount { get; init; }
    public string ProviderTransactionId { get; init; } = string.Empty;
    public string PaymentUrl { get; init; } = string.Empty;
}
