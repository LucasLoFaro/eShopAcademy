namespace Common.Domain.Commands.Basket;

public record BasketCommand : BaseCommand
{
    public Guid ClientId { get; set; }
}