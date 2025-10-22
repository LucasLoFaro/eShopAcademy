namespace Domain.Payments.Contracts;

public class PaymentResponse
{
    public string Id { get; init; } = default!;
    public double Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string ExternalId { get; init; } = default!;
    public string Url { get; init; } = default!;
    public string? FailureReason { get; init; }
}
