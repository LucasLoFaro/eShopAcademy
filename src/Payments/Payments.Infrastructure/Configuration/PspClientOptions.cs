using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

public sealed class PspClientOptions
{
    public const string SectionName = "Payment:Psp";

    [Range(1, 30)]
    public int TimeoutSeconds { get; set; } = 5;
}
