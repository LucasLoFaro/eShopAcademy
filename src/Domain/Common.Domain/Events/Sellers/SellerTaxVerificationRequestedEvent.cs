namespace Domain.Common.Events.Sellers;

public record SellerTaxVerificationRequestedEvent : SellerEvent
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string TaxId { get; init; } = string.Empty;
}
