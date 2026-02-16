namespace Domain.Common.Events.Customers;

public record CustomerAddressUpdatedEvent : CustomerEvent
{
    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string AdditionalInformation { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
}
