namespace Common.Domain.Commands.Shipping;

public record StartShippingCommand : BaseCommand
{
    public string CustomerEmail { get; init; } = string.Empty;
    public string DestinationAddress { get; init; } = string.Empty;
}
