namespace Common.Domain.Commands;

public abstract record BaseCommand : BaseMessage
{
    public Guid OrderId { get; init; }
    public override Guid CorrelationId => OrderId;
    public virtual string CommandType => GetType().Name.Replace("Command", "");
}
