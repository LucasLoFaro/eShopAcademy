namespace Common.Domain.Commands.Shipping;

public record ConfirmShippingCommand : ShippingCommand
{
    public Guid ShippingId { get; set; }
}
