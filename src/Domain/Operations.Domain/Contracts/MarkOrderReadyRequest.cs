namespace Domain.Operations.Contracts;

public record MarkOrderReadyRequest
{
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
}
