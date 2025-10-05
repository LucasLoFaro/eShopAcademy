namespace Domain.Contracts;

public class PaymentNotification
{
    public Guid Id { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string Status { get; init; } = default!;
    public Guid ExternalId { get; init; } = default!;
    public string? FailureReason { get; init; }
}
