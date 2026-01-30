namespace Common.Domain.Commands.Shipping;

public record CancelShippingCommand : BaseCommand
{
    public Guid ShippingId { get; set; }
}
