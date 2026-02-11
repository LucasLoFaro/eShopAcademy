namespace Domain.Common.Commands.Shipping;

public record ConfirmPickupCommand : ShippingCommand
{
    public Guid ShippingId { get; set; }
    public DateTime ReadyAt { get; set; }
}
