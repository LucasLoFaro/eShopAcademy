namespace Common.Domain.Commands.Payments;

public record RefundPaymentCommand : PaymentCommand
{
    public decimal Amount { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ProviderTransactionId { get; init; } = string.Empty;
}