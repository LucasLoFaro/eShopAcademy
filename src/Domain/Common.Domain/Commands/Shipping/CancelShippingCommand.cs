namespace Common.Domain.Commands.Shipping;

public record CancelShippingCommand : ShippingCommand
{
    public Guid ShippingId { get; init; }
}
