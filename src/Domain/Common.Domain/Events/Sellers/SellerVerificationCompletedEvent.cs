namespace Domain.Common.Events.Sellers;

public record SellerVerificationCompletedEvent : SellerEvent
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool Approved { get; init; }
    public string? Reason { get; init; }
}
