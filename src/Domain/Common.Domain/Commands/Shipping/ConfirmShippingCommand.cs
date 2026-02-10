namespace Domain.Common.Commands.Shipping;

public record ConfirmShippingCommand : ShippingCommand
{
    public Guid ShippingId { get; set; }
    public DateTime ReadyAt { get; set; }
}
