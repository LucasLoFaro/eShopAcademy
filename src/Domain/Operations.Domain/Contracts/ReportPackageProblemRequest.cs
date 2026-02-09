namespace Domain.Operations.Contracts;

public record ReportPackageProblemRequest
{
    public string? IssueType { get; init; }
    public string? Details { get; init; }
    public string? ReportedBy { get; init; }
}
