namespace Domain.Common.Commands.Basket;

public record BasketCommand : BaseCommand
{
    public Guid ClientId { get; set; }
}