namespace Domain.Contracts;

public class PaymentResponse
{
    public Guid Id { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string ExternalId { get; init; } = default!;
    public string Url { get; init; } = default!;
}
