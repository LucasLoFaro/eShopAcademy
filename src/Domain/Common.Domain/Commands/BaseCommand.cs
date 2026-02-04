namespace Domain.Common.Commands;

public abstract record BaseCommand : BaseMessage
{
    public Guid OrderId { get; set; }
    public override Guid CorrelationId => OrderId;
    public virtual string CommandType => GetType().Name.Replace("Command", "");
}
