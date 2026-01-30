namespace Common.Domain.Commands.Shipping;

public record ConfirmShippingCommand : BaseCommand
{
    public Guid ShippingId { get; set; }
}
