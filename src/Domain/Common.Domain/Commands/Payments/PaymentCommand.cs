namespace Domain.Common.Commands.Payments;

public abstract record PaymentCommand : BaseCommand
{
    public Guid PaymentId { get; init; }
}
